# MaxAgent.py - Socket.IO 实时代理（2021+ 推荐方案）
# 放置位置：scripts\Startup\MaxAgent.py

import pymxs
import socketio
import threading
import time
import traceback

rt = pymxs.runtime

# ==================== 配置 ====================
SERVER_URL = 'http://127.0.0.1:8765'          # 改成你的服务器地址/端口
RECONNECT_DELAY = 5                           # 断线重连间隔（秒）
NAMESPACE = '/'                               # 默认命名空间

sio = socketio.Client(reconnection=True, reconnection_attempts=0, reconnection_delay=RECONNECT_DELAY)

log_enabled = True

def log(msg):
    if not log_enabled:
        return
    ts = time.strftime("%Y-%m-%d %H:%M:%S")
    line = f"{ts} | {msg}"
    print(line)
    # 可选：写入文件
    # with open(r"C:\temp\MaxAgent_log.txt", "a", encoding="utf-8") as f:
    #     f.write(line + "\n")

# ==================== QF 接口（可自由扩展） ====================
class QF:
    @staticmethod
    def TestSuccess(message="测试成功"):
        log(f"QF.TestSuccess 被调用: {message}")
        rt.messageBox(f"成功！\n{message}\n\n来自 Socket.IO 的实时调用！",
                      title="QF 测试成功", beep=False)
        return "OK"

    @staticmethod
    def CreateBox(name="MyBox", pos=[0,0,0], size=[50,50,50]):
        log(f"创建 Box: {name}")
        box = rt.Box()
        box.name = name
        box.pos = rt.Point3(*pos)
        box.width = size[0]
        box.length = size[1]
        box.height = size[2]
        rt.redrawViews()
        return f"Box created: {name}"

    @staticmethod
    def ExecuteMaxScript(code):
        try:
            result = rt.execute(code)
            return str(result) if result is not None else "Executed (no return)"
        except Exception as ex:
            return f"Error: {str(ex)}"

    @staticmethod
    def GetSceneObjects():
        objs = [node.name for node in rt.objects]
        return objs

# 注册到 rt（可选，在 Listener 中可直接 QF.xxx() 测试）
rt.QF = QF

# ==================== Socket.IO 事件处理 ====================
@sio.event
def connect():
    log("=== 已连接到外部 Socket.IO 服务器 ===")
    sio.emit('max_connected', {'status': 'online', 'max_version': str(rt.maxVersion())})

@sio.event
def disconnect():
    log("与服务器断开连接，将尝试重连...")

@sio.event
def connect_error(data):
    log(f"连接错误: {data}")

# 核心指令接收事件（外部发 'command' 事件）
@sio.on('command')
def on_command(data):
    log(f"收到指令: {data}")
    try:
        cmd_type = data.get('type', 'unknown')
        payload = data.get('payload', {})

        result = None
        error = None

        if cmd_type == 'qf_call':
            method_name = payload.get('method')
            args = payload.get('args', {})
            if hasattr(QF, method_name):
                try:
                    func = getattr(QF, method_name)
                    result = func(**args)
                except Exception as ex:
                    error = str(ex)
                    traceback.print_exc()
            else:
                error = f"QF 方法不存在: {method_name}"

        elif cmd_type == 'execute':
            code = payload.get('code', '')
            try:
                result = rt.execute(code)
            except Exception as ex:
                error = str(ex)

        elif cmd_type == 'query':
            query_type = payload.get('query_type')
            if query_type == 'objects':
                result = [node.name for node in rt.objects]

        else:
            error = f"未知指令类型: {cmd_type}"

        # 回传结果（双向）
        response = {
            'original': data,
            'result': result,
            'error': error,
            'timestamp': time.time()
        }
        sio.emit('command_result', response)
        log(f"指令执行完成 → 结果已回传: {response}")

    except Exception as e:
        log(f"指令处理异常: {e}")
        sio.emit('command_result', {'error': str(e), 'original': data})

# ==================== 后台连接线程 ====================
def socketio_connect_loop():
    log(f"尝试连接 Socket.IO 服务器: {SERVER_URL}")
    while True:
        try:
            sio.connect(SERVER_URL, namespaces=[NAMESPACE])
            sio.wait()  # 阻塞直到断开
        except Exception as e:
            log(f"连接失败: {e}，{RECONNECT_DELAY}秒后重试...")
            time.sleep(RECONNECT_DELAY)

# 启动
log("=== MaxAgent Socket.IO 实时代理启动 ===")
threading.Thread(target=socketio_connect_loop, daemon=True).start()