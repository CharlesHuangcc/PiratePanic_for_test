import subprocess
import os
from pathlib import Path

PROJECT_PATH=Path(__file__).parent.parent
GAME_EXE=PROJECT_PATH / r"unity-sampleproject-master\PiratePanic\Build\TestPiratePanic.exe"




def run():
    if not os.path.exists(GAME_EXE):
        print(f"游戏启动程序不存在{str(GAME_EXE)}")
        return False

    game_proc = None
    try:
        game_proc = subprocess.Popen(str(GAME_EXE),)
        print(f"游戏已启动，PID={game_proc.pid}")
        # game_proc.wait(timeout=100) # 无论启动成功与否，设置进程启动时长，超出时间自动杀进程
        game_proc.wait() # 不设置启动时长，进程关闭后再执行后续内容

        ret_code = game_proc.returncode
        if ret_code == 0:
            print("游戏正常退出")
            return True
        else:
            print(f"游戏异常退出，返回码: {ret_code}")
            return False
    except subprocess.TimeoutExpired:
        print(f"进程启动达设置时长")
        if game_proc is not None:
            game_proc.kill()
        return False
    except Exception as e:
        print(f"进程启动发生未知异常：\n{e}")
        return False

if __name__=="__main__":
    run()