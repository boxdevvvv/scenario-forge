"""Turn a recorder folder into an optimized GIF.

Frames whose change from the last kept frame is tiny (caret blinks) are merged. Long pauses in the raw
recording mark key states and are held for `hold` seconds; everything else plays quickly.
Usage: makegif.py <frames dir> <out.gif> [width=900] [hold=0.9] [min_changed_pixels=20] [skip_until_frame=0] [stop_after_frame] [fast=0.08] [cuts=a-b,c-d]
"""
import re, subprocess, sys, pathlib
import numpy as np
from PIL import Image
import imageio_ffmpeg

src = pathlib.Path(sys.argv[1]); out = pathlib.Path(sys.argv[2])
width = int(sys.argv[3]) if len(sys.argv) > 3 else 900
hold = float(sys.argv[4]) if len(sys.argv) > 4 else 0.9
min_changed = int(sys.argv[5]) if len(sys.argv) > 5 else 20
skip_until = int(sys.argv[6]) if len(sys.argv) > 6 else 0
stop_after = int(sys.argv[7]) if len(sys.argv) > 7 else 10**9
fast = float(sys.argv[8]) if len(sys.argv) > 8 else 0.08
cuts = [tuple(map(int, r.split("-"))) for r in sys.argv[9].split(",")] if len(sys.argv) > 9 and sys.argv[9] else []

entries, cur = [], None
for line in (src / "frames.txt").read_text().splitlines():
    m = re.match(r"file 'f(\d+)\.png'", line)
    if m: cur = [int(m.group(1)), 0.0]; entries.append(cur); continue
    m = re.match(r"duration (.+)", line)
    if m and cur: cur[1] = float(m.group(1))
entries = [e for e in entries if skip_until <= e[0] <= stop_after and not any(a <= e[0] <= b for a, b in cuts)]

def small(index):
    img = Image.open(src / f"f{index:05d}.png").convert("L")
    return np.asarray(img.resize((img.width // 2, img.height // 2)), dtype=np.int16)

kept, last = [], None
for index, dur in entries:
    if last is not None and kept and index == kept[-1][0]:
        kept[-1][1] += dur; continue
    frame = small(index)
    if last is not None and np.count_nonzero(np.abs(frame - last) > 24) < min_changed:
        kept[-1][1] += dur; continue
    kept.append([index, dur]); last = frame

for i, k in enumerate(kept):
    if hold < 0:  # real time: keep the recorded pacing
        k[1] = 2.5 if i == len(kept) - 1 else min(k[1], 1.5)
        continue
    if i == 0: k[1] = 1.0
    elif i == len(kept) - 1: k[1] = 2.5
    elif k[1] >= 1.5: k[1] = hold
    else: k[1] = min(max(k[1], 0.04), fast)

intro = sys.argv[10] if len(sys.argv) > 10 else ""
listing = (f"file '{intro.split('@')[0]}'\nduration {float(intro.split('@')[1]):.3f}\n" if intro else "") + "".join(f"file 'f{i:05d}.png'\nduration {d:.3f}\n" for i, d in kept) + f"file 'f{kept[-1][0]:05d}.png'\n"
(src / "gif.txt").write_text(listing)
ff = imageio_ffmpeg.get_ffmpeg_exe()
vf = (f"vflip,scale={width}:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=full:max_colors=256:reserve_transparent=0[p];"
      "[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle")
subprocess.run([ff, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(src / "gif.txt"),
                "-vf", vf, "-fps_mode", "vfr", str(out)], check=True)
print(f"{len(entries)} frames -> {len(kept)} kept, {sum(d for _, d in kept):.1f}s, {out.stat().st_size / 1e6:.2f} MB")
