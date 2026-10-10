"""Deterministic original CC0 combat sound effects; Python standard library only.
python Tools/build_combat_audio.py Assets/Resources/Audio/Combat
"""
import sys,math,random,wave,struct,json,hashlib
from pathlib import Path
out=Path(sys.argv[1]);out.mkdir(parents=True,exist_ok=True);rate=22050
sounds={'SwordSwing':(.26,180),'HeavySwing':(.34,100),'DaggerSwipe':(.18,240),'BowDraw':(.36,90),'BowRelease':(.28,330),'Fire':(.46,100),'Cold':(.48,660),'Poison':(.40,140),'Lightning':(.32,400),'Light':(.48,440),'Charge':(.34,160),'Parry':(.44,710),'Dodge':(.22,160),'Hit':(.24,85),'Guard':(.30,260),'BossSlam':(.68,65),'HoundGrowl':(.62,75),'HoundBite':(.25,105),'Heal':(.45,330),'Victory':(.85,220),'Defeat':(.65,80)}
reports=[]
for name,(duration,freq) in sounds.items():
    rng=random.Random(4701+sum(ord(c)*(i+1) for i,c in enumerate(name)));samples=[];smooth=0;phase=0
    for i in range(round(duration*rate)):
        t=i/rate;p=t/duration;n=rng.uniform(-1,1);smooth=.88*smooth+.12*n
        attack=min(1,t/.006);tail=min(1,(duration-t)/.035)
        decay=math.exp(-p*5);value=0
        if name in ['SwordSwing','HeavySwing','DaggerSwipe','Dodge']:
            env=math.sin(math.pi*p)**1.8
            value=(n*.26+smooth*1.5)*env+math.sin(math.tau*(freq*t+50*t*t))*.06*env
        elif name=='BowDraw':value=(smooth*.65+math.sin(math.tau*(freq*t+30*t*t))*.08)*math.sin(math.pi*p)
        elif name=='BowRelease':value=decay*(math.sin(math.tau*freq*t)*.45+math.sin(math.tau*freq*2.05*t)*.20+n*.13)
        elif name in ['Hit','BossSlam','HoundBite','Defeat']:
            phase+=math.tau*(freq*(1-.55*p))/rate
            value=decay*(math.sin(phase)*.7+smooth*.7+n*.13)
            if name=='BossSlam':value+=n*.25*math.exp(-p*16)+math.sin(math.tau*190*t)*.14*math.exp(-p*8)
        elif name in ['Parry','Guard']:
            value=sum(math.sin(math.tau*freq*r*t)*a*math.exp(-p*d) for r,a,d in [(1,.30,5),(1.47,.23,7),(2.11,.13,9),(2.79,.07,10)])+n*.18*math.exp(-p*22)
        elif name=='HoundGrowl':
            phase+=math.tau*(freq+math.sin(t*27)*8)/rate
            voice=math.tanh(math.sin(phase)*2+math.sin(phase*2)*.6)
            value=(voice*.35+smooth*.9)*math.sin(math.pi*p)**.6*(.8+.2*math.sin(t*49))
        elif name=='Fire':value=decay*(smooth*1.4+n*.16+math.sin(math.tau*freq*t)*.3)
        elif name=='Lightning':value=decay*(n*.45*(.4+.6*abs(math.sin(t*97)))+math.sin(math.tau*freq*t)*.12)
        elif name=='Poison':value=(smooth*.65+math.sin(math.tau*(freq*t+math.sin(t*35)*.35))*.24)*math.sin(math.pi*p)*(.55+.45*math.sin(t*42)**2)
        elif name=='Charge':value=(smooth*.7+math.sin(math.tau*(freq*t+180*t*t))*.14)*math.sin(math.pi*p)*p
        elif name in ['Cold','Light','Heal']:
            ratios=[1,1.5,2.12] if name=='Cold' else [1,1.25,1.5]
            value=sum(math.sin(math.tau*freq*r*t)*a for r,a in zip(ratios,[.27,.15,.09]))*decay+smooth*.3*math.sin(math.pi*p)
        elif name=='Victory':value=sum(math.sin(math.tau*freq*r*t)*.18 for r in [1,1.1892,1.4983])*math.sin(math.pi*p)*math.exp(-p*1.2)
        samples.append(value*attack*tail)
    peak=max(abs(v) for v in samples);gain=.62/max(peak,.01)
    pcm=b''.join(struct.pack('<h',round(v*gain*32767)) for v in samples)
    path=out/(name+'.wav')
    with wave.open(str(path),'wb') as wav:wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(rate);wav.writeframes(pcm)
    reports.append({'name':name,'duration':len(samples)/rate,'sample_rate':rate,'peak':.62,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
(out/'AUDIO_REPORT.json').write_text(json.dumps(reports,indent=2)+'\n');print('Generated',len(reports),'original mono PCM sound effects')
