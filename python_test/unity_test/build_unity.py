import subprocess
import time
from pathlib import Path

PYTHON_PROJECT_PATH=Path(__file__).parent
UNITY=r"C:\Program Files\Unity\Hub\Editor\2020.3.14f1c1\Editor\Unity.exe"
PROJECT_PATH=PYTHON_PROJECT_PATH.parent / r"unity-sampleproject-master\PiratePanic"
BUILD_OUTPUT=PROJECT_PATH / r"Build"
SCENES=[
    r"Assets\PiratePanic\Scenes\Scene01MainMenu.unity",
    r"Assets\PiratePanic\Scenes\Scene02Battle.unity"
]
GAME_EXE_NAME = "TestPiratePanic.exe"

def build_unity_project():
    scene_args = []
    for scene in SCENES:
        scene_args.append("-scene")
        scene_args.append(scene)

    cmd = [
        str(UNITY),
        "-batchmode",  # 静默批处理模式，不打开窗口
        "-nographics",  # 无图形界面
        "-projectPath", str(PROJECT_PATH),
        "-buildWindowsPlayer", str(BUILD_OUTPUT / GAME_EXE_NAME),
        "-quit"  # 打包完成自动退出Unity
    ]

    cmd=cmd[:-1] + scene_args + cmd[-1:] # 完整命令

    proc=subprocess.Popen(cmd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
    stdout, stderr = proc.communicate()

    with open(str(PYTHON_PROJECT_PATH / f"unity_build_log_{time.strftime("%m月%d日%H_%M")}.txt"),'a',encoding="utf-8") as f:
        if stdout:
            f.write(f"===================正常打印=================：\n"+stdout)
        if stderr:
            f.write(f"===================异常打印=================：\n"+stderr)

    if proc.returncode == 0:
        print("打包成功！")
        return True
    else:
        print("打包失败，返回码：", proc.returncode)
        return False


if __name__ == "__main__":
    build_unity_project()