"""Compare our C# s2twp against Python opencc (opencc-python-reimplemented or opencc)."""
import subprocess, sys
from opencc import OpenCC
SAMPLES = "软件 信息 网络 鼠标 打印机 这里 头发 里面 游戏 设置 保存 钓鱼 钥匙 内存 视频 服务器 程序 菜单 窗口 发现 干净 面条 后来 皇后 一只 台风 着陆 背包 任务 岛屿".split()
SAMPLES += ["我们在岛上钓鱼，然后回到里面保存游戏。", "请在设置里调整鼠标灵敏度。"]
out = subprocess.run(["dotnet", "run", "--project", "tools/SelfCheck", "--"], input="\n".join(SAMPLES) + "\n",
                     capture_output=True, text=True, encoding="utf-8", check=True).stdout.splitlines()
cc = OpenCC("s2twp")
diff = 0
for s, ours in zip(SAMPLES, out):
    ref = cc.convert(s)
    mark = "OK " if ref == ours else "DIFF"
    diff += ref != ours
    print(f"{mark} {s} -> ours {ours} | python {ref}")
print(f"{len(SAMPLES) - diff}/{len(SAMPLES)} identical")
