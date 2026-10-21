from pathlib import Path
from html import escape
import math
import sys
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).parent
ENGLISH = '--english' in sys.argv
STEM = 'teacher-attendance-swimlane-en' if ENGLISH else 'teacher-attendance-swimlane'
TRANSLATIONS = {
    'Bắt đầu': 'Start',
    'Đăng nhập': 'Log in',
    'Chọn chức năng\nĐiểm danh theo tiết': 'Select the\nattendance function',
    'Chọn ngày và tiết\nđược phân công': 'Choose a date and\nassigned lesson',
    'Chọn kết quả từng\nhọc sinh và ghi chú': 'Mark each student\nand add comments',
    'Hoàn tất\nđiểm danh?': 'Marking\ncomplete?',
    'Nhấn xác nhận /\nLưu chỉnh sửa': 'Submit attendance /\nSave changes',
    'Xem kết quả và\nthông báo\nthành công': 'View results and\nsuccess notification',
    'Điểm danh\ntiết khác?': 'Another\nlesson?',
    'Kết thúc': 'End',
    'Xác thực tài khoản\nvà vai trò giáo viên': 'Authenticate user\nand teacher role',
    'Lấy danh sách tiết\ncủa giáo viên': 'Fetch the teacher\'s\nassigned lessons',
    'Tải bảng, danh sách\nhọc sinh và đơn nghỉ\nđã được duyệt': 'Load attendance,\nstudent roster and\napproved leave',
    'Được phép\nnhập / sửa?': 'Editing\nallowed?',
    'Chỉ xem / Báo lý do\nchưa thể điểm danh': 'Show read-only view /\nExplain restriction',
    'Kiểm tra điều kiện\nvà dữ liệu điểm danh': 'Recheck conditions\nand attendance data',
    'Hợp lệ?': 'Valid?',
    'Lưu kết quả, lịch sử;\nđặt trạng thái Đã chốt': 'Save results and audit;\nset status to Submitted',
    'Xác thực': 'Authenticate',
    'Đúng': 'Valid',
    'Yêu cầu danh sách': 'Request list',
    'Hiển thị': 'Display',
    'Mở bảng': 'Open sheet',
    'Có': 'Yes',
    'Không': 'No',
    'Chưa': 'No',
    'Gửi kết quả': 'Submit',
    'Không: báo lỗi, kiểm tra / tải lại': 'No: show error; review / reload',
    'GIÁO VIÊN ĐIỂM DANH THEO TIẾT': 'TEACHER ATTENDANCE WORKFLOW',
    'ĐIỂM DANH': 'ATTENDANCE',
    'GIÁO VIÊN': 'TEACHER',
    'HỆ THỐNG': 'SYSTEM',
    'Giáo viên điểm danh': 'Teacher Attendance',
    'Kết quả: Có mặt · Đi muộn · Vắng không phép · Vắng có phép.  Bảng điểm danh do Staff tạo trước.': 'Statuses: Present · Late · Absent · Excused. Attendance sessions must be created by Staff beforehand.',
    'Chỉ nhập / sửa từ giờ bắt đầu tiết đến trước 00:00 ngày kế tiếp (giờ Việt Nam), khi học kỳ mở và bảng chưa hủy. Sửa bảng đã chốt phải có lý do.': 'Entry / editing is allowed from the lesson start until before 00:00 the next day (Vietnam time), while the semester is active and the session is not cancelled. Changes after submission require a reason.',
    'Kết quả: Có mặt · Đi muộn · Vắng không phép · Vắng có phép. Bảng do Staff tạo trước.\nChỉ nhập/sửa từ giờ bắt đầu tiết đến trước 00:00 ngày kế tiếp (giờ Việt Nam); học kỳ mở, bảng chưa hủy. Sửa bảng đã chốt phải có lý do.': 'Statuses: Present · Late · Absent · Excused. Sessions must be created by Staff beforehand.\nEntry / editing is allowed from the lesson start until before 00:00 the next day (Vietnam time), while the semester is active and the session is not cancelled. Changes after submission require a reason.',
}

def tr(value):
    return TRANSLATIONS.get(value, value) if ENGLISH else value

W, H = 3140, 865
nodes = []
edges = []

def node(key, x, y, text, kind='box', w=190, h=90, lane='teacher'):
    if kind in ('start', 'end'):
        w = h = 58
    if kind == 'decision':
        w, h = 120, 120
    nodes.append(dict(id=key, x=x, y=y, text=text, kind=kind, w=w, h=h, lane=lane))

def edge(a, b, points, label='', label_pos=None):
    edges.append(dict(a=a, b=b, points=points, label=label, label_pos=label_pos))

node('start', 135, 295, 'Bắt đầu', 'start')
node('login', 280, 295, 'Đăng nhập')
node('menu', 515, 295, 'Chọn chức năng\nĐiểm danh theo tiết')
node('choose', 755, 295, 'Chọn ngày và tiết\nđược phân công')
node('mark', 1170, 295, 'Chọn kết quả từng\nhọc sinh và ghi chú')
node('complete', 1410, 295, 'Hoàn tất\nđiểm danh?', 'decision')
node('save', 1630, 295, 'Nhấn xác nhận /\nLưu chỉnh sửa')
node('view', 2470, 295, 'Xem kết quả và\nthông báo\nthành công')
node('another', 2760, 295, 'Điểm danh\ntiết khác?', 'decision')
node('end', 3010, 295, 'Kết thúc', 'end')
node('auth', 280, 570, 'Xác thực tài khoản\nvà vai trò giáo viên', lane='system')
node('list', 515, 570, 'Lấy danh sách tiết\ncủa giáo viên', lane='system')
node('load', 755, 570, 'Tải bảng, danh sách\nhọc sinh và đơn nghỉ\nđã được duyệt', h=100, lane='system')
node('writable', 980, 570, 'Được phép\nnhập / sửa?', 'decision', lane='system')
node('readonly', 980, 707, 'Chỉ xem / Báo lý do\nchưa thể điểm danh', w=225, h=72, lane='system')
node('blocked_end', 1210, 707, 'Kết thúc', 'end', lane='system')
node('validate', 1630, 570, 'Kiểm tra điều kiện\nvà dữ liệu điểm danh', w=210, lane='system')
node('valid', 1895, 570, 'Hợp lệ?', 'decision', lane='system')
node('commit', 2190, 570, 'Lưu kết quả, lịch sử;\nđặt trạng thái Đã chốt', w=230, lane='system')

edge('start','login',[(164,295),(185,295)])
edge('login','menu',[(375,295),(420,295)])
edge('login','auth',[(280,340),(280,525)],'Xác thực',(325,442))
edge('auth','menu',[(375,570),(400,570),(400,295),(420,295)],'Đúng',(371,390))
# The menu becomes available after successful authentication.
edges.pop(1)
edge('menu','list',[(515,340),(515,525)],'Yêu cầu danh sách',(598,442))
edge('list','choose',[(610,570),(637,570),(637,295),(660,295)],'Hiển thị',(678,382))
edge('choose','load',[(755,340),(755,520)],'Mở bảng',(803,442))
edge('load','writable',[(850,570),(920,570)])
edge('writable','mark',[(1040,570),(1060,570),(1060,295),(1075,295)],'Có',(1080,468))
edge('writable','readonly',[(980,630),(980,671)],'Không',(1023,652))
edge('readonly','blocked_end',[(1092.5,707),(1181,707)])
edge('mark','complete',[(1265,295),(1350,295)])
edge('complete','mark',[(1410,235),(1410,175),(1170,175),(1170,250)],'Chưa',(1290,163))
edge('complete','save',[(1470,295),(1535,295)],'Có',(1502,279))
edge('save','validate',[(1630,340),(1630,525)],'Gửi kết quả',(1684,442))
edge('validate','valid',[(1735,570),(1835,570)])
edge('valid','mark',[(1895,510),(1895,205),(1290,205),(1290,360),(1170,360),(1170,340)],'Không: báo lỗi, kiểm tra / tải lại',(1628,191))
edge('valid','commit',[(1955,570),(2075,570)],'Có',(2014,554))
edge('commit','view',[(2305,570),(2340,570),(2340,295),(2375,295)])
edge('view','another',[(2565,295),(2700,295)])
edge('another','choose',[(2760,235),(2760,128),(755,128),(755,250)],'Có',(2535,114))
edge('another','end',[(2820,295),(2981,295)],'Không',(2898,279))

im = Image.new('RGB', (W,H), 'white')
d = ImageDraw.Draw(im)
font_path = 'C:/Windows/Fonts/arial.ttf'
font = ImageFont.truetype(font_path,20)
circle_font = ImageFont.truetype(font_path,15)
small = ImageFont.truetype(font_path,18)
title_font = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',30)
svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
       '<title>Teacher attendance swimlane diagram</title>' if ENGLISH else '<title>Swimlane — Giáo viên điểm danh theo tiết</title>',
       '<desc>Teacher and System lanes: log in, choose a lesson, check editing permission, mark students, submit, validate, save and view results.</desc>' if ENGLISH else '<desc>Hai làn ngang Giáo viên và Hệ thống. Đăng nhập, chọn tiết, kiểm tra quyền nhập, ghi nhận, xác nhận, kiểm tra dữ liệu, lưu và xem kết quả.</desc>',
       '<defs><pattern id="grid" width="20" height="20" patternUnits="userSpaceOnUse"><path d="M20 0H0V20" fill="none" stroke="#e9e9e9" stroke-width="0.7"/></pattern><marker id="arrow" markerWidth="9" markerHeight="7" refX="8" refY="3.5" orient="auto"><path d="M0 0L9 3.5L0 7Z" fill="#222"/></marker></defs>',
       '<rect width="100%" height="100%" fill="white"/>',
       '<rect x="25" y="88" width="3090" height="690" fill="url(#grid)"/>']

for x in range(25,3116,20): d.line([(x,88),(x,778)],fill='#eeeeee')
for y in range(88,779,20): d.line([(25,y),(3115,y)],fill='#eeeeee')

def line(points, width=2):
    d.line(points,fill='#333333',width=width)
    svg.append('<polyline points="'+ ' '.join(f'{x},{y}' for x,y in points)+'" fill="none" stroke="#333" stroke-width="1.6"/>')

def text(x,y,value,f=font,size=20,weight='normal'):
    value = tr(value)
    lines=value.split('\n')
    gap=size+7
    for i,t in enumerate(lines):
        cy=y+(i-(len(lines)-1)/2)*gap
        d.text((x,cy),t,font=f,fill='#111111',anchor='mm')
        svg.append(f'<text x="{x}" y="{cy}" text-anchor="middle" dominant-baseline="central" font-family="Arial,sans-serif" font-size="{size}" font-weight="{weight}" fill="#111">{escape(t)}</text>')

text(W/2,40,'GIÁO VIÊN ĐIỂM DANH THEO TIẾT',title_font,30,'bold')
line([(25,88),(3115,88),(3115,778),(25,778),(25,88)])
line([(61,88),(61,778)])
line([(98,88),(98,778)])
line([(61,425),(3115,425)])

def rotated(x,y,value):
    value = tr(value)
    layer=Image.new('RGBA',(700,50),(255,255,255,0))
    ImageDraw.Draw(layer).text((350,25),value,font=font,fill='#111111',anchor='mm')
    layer=layer.rotate(90,expand=True)
    im.paste(layer,(int(x-layer.width/2),int(y-layer.height/2)),layer)
    svg.append(f'<text transform="translate({x},{y}) rotate(-90)" text-anchor="middle" dominant-baseline="central" font-family="Arial,sans-serif" font-size="22">{escape(value)}</text>')

rotated(43,433,'ĐIỂM DANH')
rotated(80,257,'GIÁO VIÊN')
rotated(80,601,'HỆ THỐNG')

for e in edges:
    pts=e['points']
    d.line(pts,fill='#222222',width=2)
    (x0,y0),(x1,y1)=pts[-2:]
    angle=math.atan2(y1-y0,x1-x0)
    arrow=[(x1,y1),(x1-10*math.cos(angle)+4*math.sin(angle),y1-10*math.sin(angle)-4*math.cos(angle)),(x1-10*math.cos(angle)-4*math.sin(angle),y1-10*math.sin(angle)+4*math.cos(angle))]
    d.polygon(arrow,fill='#222222')
    svg.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in pts)+'" fill="none" stroke="#222" stroke-width="1.7" marker-end="url(#arrow)"/>')
    if e['label']:
        text(*e['label_pos'],e['label'],small,18)

for n in nodes:
    x,y,w,h=n['x'],n['y'],n['w'],n['h']
    box=(x-w/2,y-h/2,x+w/2,y+h/2)
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
    is_circle = n['kind'] in ('start','end')
    text(x,y,n['text'],circle_font if is_circle else font,15 if is_circle else 20)

text(W/2,813,'Kết quả: Có mặt · Đi muộn · Vắng không phép · Vắng có phép.  Bảng điểm danh do Staff tạo trước.',small,18)
text(W/2,841,'Chỉ nhập / sửa từ giờ bắt đầu tiết đến trước 00:00 ngày kế tiếp (giờ Việt Nam), khi học kỳ mở và bảng chưa hủy. Sửa bảng đã chốt phải có lý do.',small,18)
svg.append('</svg>')
(OUT/f'{STEM}.svg').write_text('\n'.join(svg),encoding='utf-8')
im.save(OUT/f'{STEM}.png')

# Editable diagrams.net source with native swimlanes, activities and connectors.
mx=ET.Element('mxfile',host='app.diagrams.net')
diagram=ET.SubElement(mx,'diagram',id='teacher-attendance',name=tr('Giáo viên điểm danh'))
model=ET.SubElement(diagram,'mxGraphModel',dx=str(W),dy=str(H),grid='1',gridSize='20',page='1',pageWidth=str(W),pageHeight=str(H))
root=ET.SubElement(model,'root')
ET.SubElement(root,'mxCell',id='0')
ET.SubElement(root,'mxCell',id='1',parent='0')

def cell(key,value,style,parent,x,y,w,h):
    c=ET.SubElement(root,'mxCell',id=key,value=tr(value),style=style,vertex='1',parent=parent)
    ET.SubElement(c,'mxGeometry',x=str(x),y=str(y),width=str(w),height=str(h),attrib={'as':'geometry'})
    return c

base='fontFamily=Arial;fontSize=20;fontColor=#111111;strokeColor=#333333;fillColor=#ffffff;whiteSpace=wrap;html=0;'
cell('title','GIÁO VIÊN ĐIỂM DANH THEO TIẾT','text;html=0;align=center;fontFamily=Arial;fontSize=30;fontStyle=1;strokeColor=none;fillColor=none;','1',25,10,3090,55)
cell('pool','ĐIỂM DANH','swimlane;horizontal=0;startSize=36;collapsible=0;'+base,'1',25,88,3090,690)
cell('teacher','GIÁO VIÊN','swimlane;horizontal=0;startSize=37;collapsible=0;'+base,'pool',36,0,3054,337)
cell('system','HỆ THỐNG','swimlane;horizontal=0;startSize=37;collapsible=0;'+base,'pool',36,337,3054,353)
for n in nodes:
    shape={'box':'rounded=1;arcSize=12;','decision':'rhombus;','start':'ellipse;','end':'ellipse;'}[n['kind']]
    yy=88 if n['lane']=='teacher' else 425
    cell(n['id'],n['text'],shape+base+('fontSize=15;' if n['kind'] in ('start','end') else ''),n['lane'],n['x']-61-n['w']/2,n['y']-yy-n['h']/2,n['w'],n['h'])

for i,e in enumerate(edges):
    ns={n['id']:n for n in nodes}
    src,tgt=ns[e['a']],ns[e['b']]
    p0,p1=e['points'][0],e['points'][-1]
    ex=(p0[0]-(src['x']-src['w']/2))/src['w']; ey=(p0[1]-(src['y']-src['h']/2))/src['h']
    ix=(p1[0]-(tgt['x']-tgt['w']/2))/tgt['w']; iy=(p1[1]-(tgt['y']-tgt['h']/2))/tgt['h']
    style=f'edgeStyle=none;rounded=0;html=0;endArrow=block;endFill=1;strokeColor=#222222;fontFamily=Arial;fontSize=18;labelBackgroundColor=#ffffff;exitX={ex};exitY={ey};exitDx=0;exitDy=0;entryX={ix};entryY={iy};entryDx=0;entryDy=0;'
    c=ET.SubElement(root,'mxCell',id='edge'+str(i),value=tr(e['label']),style=style,edge='1',parent='1',source=e['a'],target=e['b'])
    g=ET.SubElement(c,'mxGeometry',relative='1',attrib={'as':'geometry'})
    if len(e['points'])>2:
        arr=ET.SubElement(g,'Array',attrib={'as':'points'})
        for x,y in e['points'][1:-1]: ET.SubElement(arr,'mxPoint',x=str(x),y=str(y))

cell('note','Kết quả: Có mặt · Đi muộn · Vắng không phép · Vắng có phép. Bảng do Staff tạo trước.\nChỉ nhập/sửa từ giờ bắt đầu tiết đến trước 00:00 ngày kế tiếp (giờ Việt Nam); học kỳ mở, bảng chưa hủy. Sửa bảng đã chốt phải có lý do.','text;html=0;align=center;fontFamily=Arial;fontSize=18;strokeColor=none;fillColor=none;','1',25,794,3090,60)
ET.ElementTree(mx).write(OUT/f'{STEM}.drawio',encoding='utf-8',xml_declaration=True)
print('Created PNG, SVG and editable drawio in',OUT)
