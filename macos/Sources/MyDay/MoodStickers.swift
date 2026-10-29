import SwiftUI

enum MoodStickerCatalog {
    static let moods=["행복해요","설레요","평온해요","피곤해요","속상해요"]
    static let colors=[
        Color(red:154/255,green:174/255,blue:105/255),
        Color(red:194/255,green:210/255,blue:151/255),
        Color(red:255/255,green:217/255,blue:139/255),
        Color(red:242/255,green:157/255,blue:107/255),
        Color(red:231/255,green:78/255,blue:76/255)
    ]
    static func index(_ mood:String)->Int {moods.firstIndex(of:mood) ?? 2}
}

struct MoodSticker:View {
    let mood:String
    var size:CGFloat=38
    private var index:Int {MoodStickerCatalog.index(mood)}
    var body:some View {
        Canvas{context,canvas in
            let scale=min(canvas.width,canvas.height),rect=CGRect(x:0,y:0,width:scale,height:scale)
            context.fill(Path(ellipseIn:rect),with:.color(MoodStickerCatalog.colors[index]))
            let ink=Color(red:55/255,green:62/255,blue:91/255)
            func ellipse(_ x:CGFloat,_ y:CGFloat,_ w:CGFloat,_ h:CGFloat){context.fill(Path(ellipseIn:CGRect(x:x*scale,y:y*scale,width:w*scale,height:h*scale)),with:.color(ink))}
            if index==0 {
                var eyes=Path();eyes.move(to:CGPoint(x:0.22*scale,y:0.42*scale));eyes.addQuadCurve(to:CGPoint(x:0.40*scale,y:0.42*scale),control:CGPoint(x:0.31*scale,y:0.29*scale));eyes.move(to:CGPoint(x:0.60*scale,y:0.42*scale));eyes.addQuadCurve(to:CGPoint(x:0.78*scale,y:0.42*scale),control:CGPoint(x:0.69*scale,y:0.29*scale));context.stroke(eyes,with:.color(ink),style:StrokeStyle(lineWidth:max(1.5,scale*0.045),lineCap:.round))
                let blush=Color(red:222/255,green:113/255,blue:96/255).opacity(0.48)
                context.fill(Path(ellipseIn:CGRect(x:0.13*scale,y:0.49*scale,width:0.14*scale,height:0.08*scale)),with:.color(blush))
                context.fill(Path(ellipseIn:CGRect(x:0.73*scale,y:0.49*scale,width:0.14*scale,height:0.08*scale)),with:.color(blush))
            } else if index==3 || index==4 {
                var eyes=Path();eyes.move(to:CGPoint(x:0.26*scale,y:0.40*scale));eyes.addQuadCurve(to:CGPoint(x:0.40*scale,y:0.40*scale),control:CGPoint(x:0.33*scale,y:0.47*scale));eyes.move(to:CGPoint(x:0.60*scale,y:0.40*scale));eyes.addQuadCurve(to:CGPoint(x:0.74*scale,y:0.40*scale),control:CGPoint(x:0.67*scale,y:0.47*scale));context.stroke(eyes,with:.color(ink),lineWidth:max(1.5,scale*0.045))
            } else {ellipse(0.27,0.35,0.11,0.11);ellipse(0.62,0.35,0.11,0.11)}
            if index==0 {
                var mouth=Path();mouth.move(to:CGPoint(x:0.28*scale,y:0.58*scale));mouth.addQuadCurve(to:CGPoint(x:0.72*scale,y:0.58*scale),control:CGPoint(x:0.50*scale,y:0.66*scale));mouth.addQuadCurve(to:CGPoint(x:0.28*scale,y:0.58*scale),control:CGPoint(x:0.50*scale,y:0.88*scale));context.fill(mouth,with:.color(ink))
            } else if index==4 {ellipse(0.31,0.59,0.38,0.22)}
            else {
                var mouth=Path();mouth.move(to:CGPoint(x:0.28*scale,y:0.60*scale))
                if index==1 {mouth.addQuadCurve(to:CGPoint(x:0.72*scale,y:0.60*scale),control:CGPoint(x:0.50*scale,y:0.78*scale))}
                else if index==2 {mouth.addLine(to:CGPoint(x:0.72*scale,y:0.60*scale))}
                else {mouth.addQuadCurve(to:CGPoint(x:0.72*scale,y:0.68*scale),control:CGPoint(x:0.50*scale,y:0.47*scale))}
                context.stroke(mouth,with:.color(ink),style:StrokeStyle(lineWidth:max(2,scale*0.065),lineCap:.round,lineJoin:.round))
            }
        }.frame(width:size,height:size).accessibilityLabel(mood)
    }
}

struct MoodStickerPicker:View {
    @Binding var mood:String
    var body:some View {
        HStack(spacing:7){
            Text("오늘의 마음").font(.caption.weight(.semibold)).foregroundStyle(DesktopPalette.muted).padding(.trailing,3)
            ForEach(MoodStickerCatalog.moods,id:\.self){value in
                Button{mood=value}label:{MoodSticker(mood:value,size:38).padding(3).background(mood==value ? DesktopPalette.paper:Color.clear,in:Circle()).overlay(Circle().stroke(mood==value ? DesktopPalette.olive:Color.clear,lineWidth:2))}.buttonStyle(.plain).help(value).accessibilityLabel("오늘의 마음 \(value)").accessibilityAddTraits(mood==value ? .isSelected:[])
            }
        }.padding(.horizontal,11).frame(height:54).background(DesktopPalette.friendCard,in:RoundedRectangle(cornerRadius:14))
    }
}

struct MoodStickerDashboard:View {
    @Binding var mood:String
    private let labels=["아주 좋아요","좋아요","보통이에요","슬퍼요","힘들어요"]
    var body:some View{
        HStack(spacing:0){
            Label("오늘의 기분",systemImage:"leaf").font(.headline).foregroundStyle(DesktopPalette.olive).frame(width:190,alignment:.leading)
            Divider().frame(height:44)
            ForEach(Array(MoodStickerCatalog.moods.enumerated()),id:\.element){index,value in
                Button{mood=value}label:{
                    VStack(spacing:5){MoodSticker(mood:value,size:44).padding(4).background(mood==value ? DesktopPalette.selection:Color.clear,in:Circle()).overlay(Circle().stroke(mood==value ? DesktopPalette.olive:Color.clear,lineWidth:2));Text(labels[index]).font(.caption).foregroundStyle(mood==value ? DesktopPalette.olive:DesktopPalette.ink)}.frame(maxWidth:.infinity)
                }.buttonStyle(.plain).help(value).accessibilityLabel("오늘의 기분 \(labels[index])").accessibilityAddTraits(mood==value ? .isSelected:[])
            }
        }.padding(.horizontal,18).frame(minHeight:100).background(DesktopPalette.paper.opacity(0.82),in:RoundedRectangle(cornerRadius:16)).overlay(RoundedRectangle(cornerRadius:16).stroke(DesktopPalette.soft,lineWidth:1))
    }
}
