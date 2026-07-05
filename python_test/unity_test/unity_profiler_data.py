import socket
import json
import time

HOST = "127.0.0.1"
PORT = 8899


def connect_server():
    """建立连接，失败自动重试，返回可用socket"""
    while True:
        sock = None
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.connect((HOST, PORT))
            print("连接成功")
            return sock
        except ConnectionRefusedError:
            print("连接拒绝，1s重试...")
        except Exception as e:
            print(f"连接异常 {e}，1s重试...")
        if sock:
            sock.close()
        time.sleep(1)


# 初始化连接
s = connect_server()
buffer = b""

while True:
    try:
        data = s.recv(4096)
        # 服务端关闭连接，data为空字节
        if not data:
            raise ConnectionError("服务端主动断开连接")

        buffer += data
        # 分割换行JSON
        while b"\n" in buffer:
            line, buffer = buffer.split(b"\n", 1)
            perf = json.loads(line.decode("utf-8"))
            print(
                f"FPS: {perf['fps']:.1f}, 分配内存: {perf['totalAllocatedMB']:.2f}MB，接收内存：{perf['totalReservedMB']}MB，mono堆内存：{perf['monoHeapSizeMB']}")

    except Exception as e:
        print(f"接收数据异常：{e}，准备重连...")
        # 关闭失效套接字
        s.close()
        # 重新建立连接
        s = connect_server()
        # 清空残留缓冲区，避免旧脏数据干扰新连接
        buffer = b""
