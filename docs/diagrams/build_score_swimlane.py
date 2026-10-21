"""Render the agreed partial score-entry workflow, not application behavior."""
from pathlib import Path
from html import escape
import math
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).parent
STEM = 'teacher-score-entry-swimlane-en'
W, H = 3500, 810
TITLE = 'TEACHER SCORE ENTRY WORKFLOW'
NOTES = [
    'Partial entry is allowed: blank scores are skipped. Only newly saved scores are locked; previously locked scores remain unchanged.',
    'Locks apply to individual students within the selected assessment. Students without a saved score remain editable. There is no draft step.',
]
nodes, edges = [], []

def node(key, x, y, text, kind='box', w=210, h=100, lane='teacher'):
    if kind in ('start', 'end'):
        w = h = 60
    elif kind == 'decision':
        w, h = 130, 130
    nodes.append(dict(id=key, x=x, y=y, text=text, kind=kind, w=w, h=h, lane=lane))

def edge(a, b, points, label='', label_pos=None):
    edges.append(dict(a=a, b=b, points=points, label=label, label_pos=label_pos))

node('start', 140, 300, 'Start', 'start')
node('login', 285, 300, 'Log in', w=180)
node('menu', 540, 300, 'Open Assessment\nManagement')
node('assignment', 815, 300, 'Select Class\nand Subject')
node('assessment', 1090, 300, 'Select an\nAssessment')
node('enter', 1370, 300, 'Enter Scores and\nComments for\nUnlocked Students', w=220, h=110)
node('save', 1625, 300, 'Click Save', w=180)
node('confirm', 1845, 300, 'Confirm\nSave?', 'decision')
node('view', 2745, 300, 'View Results and\nSuccess Notification', w=230)
node('another', 3070, 300, 'Another\nAssessment?', 'decision')
node('end', 3350, 300, 'End', 'end')
node('auth', 285, 565, 'Authenticate\nCredentials', w=180, lane='system')
node('assignments', 540, 565, 'Fetch Assigned\nClasses and Subjects', lane='system')
node('assessments', 815, 565, 'Fetch Assessments\nfor the Assignment', lane='system')
node('sheet', 1090, 565, 'Load Grade Sheet\nand Per-Student\nLock Status', w=220, h=110, lane='system')
node('validate', 1845, 565, 'Validate Entered Scores\nand Recheck Row Locks\n(Blank Scores Allowed)', w=250, h=110, lane='system')
node('valid', 2135, 565, 'Scores\nValid?', 'decision', lane='system')
node('commit', 2440, 565, 'Save New Scores and\nLock Only Saved Rows', w=250, lane='system')

edge('start', 'login', [(170,300),(195,300)])
edge('login', 'auth', [(285,350),(285,515)], 'Authenticate', (349,435))
edge('auth', 'menu', [(375,565),(400,565),(400,300),(435,300)], 'Signed in', (450,390))
edge('menu', 'assignments', [(540,350),(540,515)], 'Fetch list', (584,435))
edge('assignments', 'assignment', [(645,565),(675,565),(675,300),(710,300)], 'Display list', (730,390))
edge('assignment', 'assessments', [(815,350),(815,515)], 'Fetch list', (860,435))
edge('assessments', 'assessment', [(920,565),(950,565),(950,300),(985,300)], 'Display list', (1005,390))
edge('assessment', 'sheet', [(1090,350),(1090,510)], 'Open sheet', (1150,435))
edge('sheet', 'enter', [(1200,565),(1230,565),(1230,300),(1260,300)], 'Show editable and\nread-only rows', (1340,447))
edge('enter', 'save', [(1480,300),(1535,300)])
edge('save', 'confirm', [(1715,300),(1780,300)])
edge('confirm', 'enter', [(1845,235),(1845,170),(1370,170),(1370,245)], 'No', (1610,153))
edge('confirm', 'validate', [(1845,365),(1845,510)], 'Yes', (1876,437))
edge('validate', 'valid', [(1970,565),(2070,565)])
edge('valid', 'enter', [(2135,500),(2135,210),(1510,210),(1510,382),(1370,382),(1370,355)], 'No: show validation errors', (1900,192))
edge('valid', 'commit', [(2200,565),(2315,565)], 'Yes', (2256,545))
edge('commit', 'view', [(2565,565),(2595,565),(2595,300),(2630,300)])
edge('view', 'another', [(2860,300),(3005,300)])
edge('another', 'assessment', [(3070,235),(3070,125),(1090,125),(1090,250)], 'Yes', (2835,108))
edge('another', 'end', [(3135,300),(3320,300)], 'No', (3220,280))

im = Image.new('RGB', (W,H), 'white')
d = ImageDraw.Draw(im)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 20)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
circle_font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 16)
title_font = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 30)
svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
       '<title>Teacher score entry swimlane diagram</title>',
       '<desc>Teacher and System lanes. The teacher selects an assigned class, subject and assessment, enters scores for unlocked students, and confirms saving. The system validates only entered scores, permits blanks, saves new scores and locks only saved rows. Previously locked scores remain read-only. Invalid scores return to entry. There is no draft step or whole-class completeness requirement.</desc>',
       '<defs><pattern id="grid" width="20" height="20" patternUnits="userSpaceOnUse"><path d="M20 0H0V20" fill="none" stroke="#e9e9e9" stroke-width="0.7"/></pattern><marker id="arrow" markerWidth="9" markerHeight="7" refX="8" refY="3.5" orient="auto"><path d="M0 0L9 3.5L0 7Z" fill="#222"/></marker></defs>',
       '<rect width="100%" height="100%" fill="white"/>',
       '<rect x="25" y="88" width="3450" height="640" fill="url(#grid)"/>']
for x in range(25,3476,20): d.line([(x,88),(x,728)], fill='#eeeeee')
for y in range(88,729,20): d.line([(25,y),(3475,y)], fill='#eeeeee')

def line(points):
    d.line(points, fill='#333333', width=2)
    svg.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in points)+'" fill="none" stroke="#333" stroke-width="1.6"/>')

def text(x,y,value,f=font,size=20,weight='normal'):
    lines = value.split('\n')
    for i,t in enumerate(lines):
        cy=y+(i-(len(lines)-1)/2)*(size+7)
        d.text((x,cy),t,font=f,fill='#111111',anchor='mm')
        svg.append(f'<text x="{x}" y="{cy}" text-anchor="middle" dominant-baseline="central" font-family="Arial,sans-serif" font-size="{size}" font-weight="{weight}" fill="#111">{escape(t)}</text>')

def rotated(x,y,value):
    layer = Image.new('RGBA',(700,50),(255,255,255,0))
    ImageDraw.Draw(layer).text((350,25),value,font=font,fill='#111111',anchor='mm')
    layer = layer.rotate(90,expand=True)
    im.paste(layer,(int(x-layer.width/2),int(y-layer.height/2)),layer)
    svg.append(f'<text transform="translate({x},{y}) rotate(-90)" text-anchor="middle" dominant-baseline="central" font-family="Arial,sans-serif" font-size="20">{escape(value)}</text>')

text(W/2,40,TITLE,title_font,30,'bold')
line([(25,88),(3475,88),(3475,728),(25,728),(25,88)])
line([(61,88),(61,728)])
line([(98,88),(98,728)])
line([(61,425),(3475,425)])
rotated(43,408,'SCORE ENTRY')
rotated(80,257,'TEACHER')
rotated(80,577,'SYSTEM')
for e in edges:
    pts = e['points']
    d.line(pts,fill='#222222',width=2)
    (x0,y0),(x1,y1) = pts[-2:]
    angle = math.atan2(y1-y0,x1-x0)
    d.polygon([(x1,y1),(x1-10*math.cos(angle)+4*math.sin(angle),y1-10*math.sin(angle)-4*math.cos(angle)),(x1-10*math.cos(angle)-4*math.sin(angle),y1-10*math.sin(angle)+4*math.cos(angle))],fill='#222222')
    svg.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in pts)+'" fill="none" stroke="#222" stroke-width="1.7" marker-end="url(#arrow)"/>')
    if e['label']: text(*e['label_pos'],e['label'],small,18)

for n in nodes:
    x,y,w,h = (n[k] for k in ('x','y','w','h'))
    box = (x-w/2,y-h/2,x+w/2,y+h/2)
    if n['kind']=='decision':
        pts=[(x,y-h/2),(x+w/2,y),(x,y+h/2),(x-w/2,y)]
        d.polygon(pts,fill='white',outline='#333333',width=2)
        svg.append('<polygon points="'+' '.join(f'{a},{b}' for a,b in pts)+'" fill="white" stroke="#333" stroke-width="1.6"/>')
    elif n['kind'] in ('start','end'):
        d.ellipse(box,fill='white',outline='#333333',width=2)
        svg.append(f'<ellipse cx="{x}" cy="{y}" rx="{w/2}" ry="{h/2}" fill="white" stroke="#333" stroke-width="1.6"/>')
    else:
        d.rounded_rectangle(box,radius=12,fill='white',outline='#333333',width=2)
        svg.append(f'<rect x="{x-w/2}" y="{y-h/2}" width="{w}" height="{h}" rx="12" fill="white" stroke="#333" stroke-width="1.6"/>')
    circle = n['kind'] in ('start','end')
    f = circle_font if circle else font
    for row in n['text'].split('\n'):
        assert d.textlength(row,font=f) <= w-10, (n['id'],row)
    text(x,y,n['text'],f,16 if circle else 20)
for i,note in enumerate(NOTES): text(W/2,758+i*29,note,small,18)
svg.append('</svg>')
(OUT/f'{STEM}.svg').write_text('\n'.join(svg),encoding='utf-8')
im.save(OUT/f'{STEM}.png')

# Preserve editable shapes, lanes and routed connectors in diagrams.net.
mx = ET.Element('mxfile',host='app.diagrams.net')
diagram = ET.SubElement(mx,'diagram',id='teacher-score-entry',name='Teacher Score Entry')
model = ET.SubElement(diagram,'mxGraphModel',dx=str(W),dy=str(H),grid='1',gridSize='20',page='1',pageWidth=str(W),pageHeight=str(H))
root = ET.SubElement(model,'root')
ET.SubElement(root,'mxCell',id='0')
ET.SubElement(root,'mxCell',id='1',parent='0')
def cell(key,value,style,parent,x,y,w,h):
    c=ET.SubElement(root,'mxCell',id=key,value=value,style=style,vertex='1',parent=parent)
    ET.SubElement(c,'mxGeometry',x=str(x),y=str(y),width=str(w),height=str(h),attrib={'as':'geometry'})
base='fontFamily=Arial;fontSize=20;fontColor=#111111;strokeColor=#333333;fillColor=#ffffff;whiteSpace=wrap;html=0;'
cell('title',TITLE,'text;align=center;fontFamily=Arial;fontSize=30;fontStyle=1;strokeColor=none;fillColor=none;','1',25,10,3450,55)
cell('pool','SCORE ENTRY','swimlane;horizontal=0;startSize=36;collapsible=0;'+base,'1',25,88,3450,640)
cell('teacher','TEACHER','swimlane;horizontal=0;startSize=37;collapsible=0;'+base,'pool',36,0,3414,337)
cell('system','SYSTEM','swimlane;horizontal=0;startSize=37;collapsible=0;'+base,'pool',36,337,3414,303)
for n in nodes:
    shape={'box':'rounded=1;arcSize=12;','decision':'rhombus;','start':'ellipse;','end':'ellipse;'}[n['kind']]
    yy=88 if n['lane']=='teacher' else 425
    cell(n['id'],n['text'],shape+base+('fontSize=16;' if n['kind'] in ('start','end') else ''),n['lane'],n['x']-61-n['w']/2,n['y']-yy-n['h']/2,n['w'],n['h'])
by_id={n['id']:n for n in nodes}
for i,e in enumerate(edges):
    src,tgt=by_id[e['a']],by_id[e['b']]
    p0,p1=e['points'][0],e['points'][-1]
    ex=(p0[0]-src['x']+src['w']/2)/src['w']; ey=(p0[1]-src['y']+src['h']/2)/src['h']
    ix=(p1[0]-tgt['x']+tgt['w']/2)/tgt['w']; iy=(p1[1]-tgt['y']+tgt['h']/2)/tgt['h']
    style=f'edgeStyle=none;rounded=0;html=0;endArrow=block;endFill=1;strokeColor=#222222;fontFamily=Arial;fontSize=18;labelBackgroundColor=#ffffff;exitX={ex};exitY={ey};exitDx=0;exitDy=0;entryX={ix};entryY={iy};entryDx=0;entryDy=0;'
    c=ET.SubElement(root,'mxCell',id='edge'+str(i),value=e['label'],style=style,edge='1',parent='1',source=e['a'],target=e['b'])
    g=ET.SubElement(c,'mxGeometry',relative='1',attrib={'as':'geometry'})
    if len(e['points'])>2:
        arr=ET.SubElement(g,'Array',attrib={'as':'points'})
        for x,y in e['points'][1:-1]: ET.SubElement(arr,'mxPoint',x=str(x),y=str(y))
cell('note','\n'.join(NOTES),'text;align=center;fontFamily=Arial;fontSize=18;strokeColor=none;fillColor=none;','1',25,740,3450,60)
ET.ElementTree(mx).write(OUT/f'{STEM}.drawio',encoding='utf-8',xml_declaration=True)
print('Created:',STEM+'.png, .svg, .drawio')
