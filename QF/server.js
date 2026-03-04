const { Server } = require("socket.io");

const io = new Server(8765, { cors: { origin: "*" } });

console.log("Socket.IO Server running at http://localhost:8765");

io.on("connection", (socket) => {

  console.log("Client connected:", socket.id);

  socket.on("max_connected", (data) => {

    console.log("Max 已连接:", data);

    io.emit("status", { message: "3ds Max online" });

  });

  socket.on("command", (data) => {

    console.log("收到命令:", data);

    socket.broadcast.emit("command", data);  // 转发给 Max（排除发送者）

  });

  socket.on("command_result", (data) => {

    console.log("Max 返回:", data);

    io.emit("command_result", data);  // 广播结果（C# 可收到）

  });

  socket.on("disconnect", () => {

    console.log("Client disconnected:", socket.id);

  });

});