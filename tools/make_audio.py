# Generates every sound in Assets/Audio from scratch (sine/noise synthesis), so there is nothing to license.
import wave, struct, math, random, os
out = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio")
os.makedirs(out, exist_ok=True)
SR = 22050

def save(name, samples):
    with wave.open(os.path.join(out, name + ".wav"), "w") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32000)) for s in samples))

def tone(freq, dur, vol=.5, decay=6, harm=.3):
    n = int(SR * dur); r = []
    for i in range(n):
        t = i / SR; env = min(1, t / .004) * math.exp(-decay * t / dur)
        r.append((math.sin(2*math.pi*freq*t) + harm*math.sin(4*math.pi*freq*t)) * env * vol)
    return r

def seq(notes, **k):
    r = []
    for f, d in notes: r += tone(f, d, **k)
    return r

save("pickup", seq([(1320, .06), (1760, .18)], vol=.45))
save("click", tone(880, .07, .4))
save("pause", seq([(440, .06), (330, .1)], vol=.4))
save("win", seq([(523, .12), (659, .12), (784, .12), (1047, .4)], vol=.5, decay=4))
save("lose", seq([(392, .2), (330, .2), (262, .5)], vol=.5, decay=3, harm=.5))
rnd = random.Random(5); lp = 0; hit = []
for i in range(int(SR * .25)):
    t = i / SR; lp += (rnd.uniform(-1, 1) - lp) * .25
    hit.append((lp * .9 + math.sin(2*math.pi*110*t) * .5) * math.exp(-14 * t))
save("hit", hit)
# 3D loops: whole number of cycles so they loop without a click
save("hum_goal", [(math.sin(2*math.pi*220*i/SR) + .5*math.sin(2*math.pi*330*i/SR)) * .3 * (.7 + .3*math.sin(2*math.pi*2*i/(SR*2))) for i in range(SR*2)])
save("hum_guard", [(((i*110/SR) % 1) * 2 - 1) * .18 + math.sin(2*math.pi*55*i/SR) * .25 for i in range(SR*2)])
# music: 4 chords, bass + arpeggio, 16 s loop
chords = [(220, 261.6, 329.6), (174.6, 220, 261.6), (196, 246.9, 293.7), (164.8, 207.7, 246.9)]
beat = .5; mus = [0.0] * int(SR * 16)
for bar, ch in enumerate(chords):
    for k in range(8):
        f = ch[k % 3] * 2 if k % 2 else ch[(k // 2) % 3]
        start = int(SR * (bar * 4 + k * beat / 1.0 * 1.0 / 2 * 2 * .5 * 1.0))
        for j, s in enumerate(tone(f, .45, .16, 5)):
            if start + j < len(mus): mus[start + j] += s
    for j, s in enumerate(tone(ch[0] / 2, 4, .22, 1.2, .1)):
        idx = int(SR * bar * 4) + j
        if idx < len(mus): mus[idx] += s
save("music", mus)
