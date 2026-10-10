using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class TemplateGallery : Form
    {
        private readonly FlowLayoutPanel list=new FlowLayoutPanel { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, WrapContents=false, AutoScroll=true, BackColor=Color.White, Padding=Design.Pad(12) };
        private readonly FlowLayoutPanel preview=new FlowLayoutPanel { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, WrapContents=false, AutoScroll=true, Padding=Design.Pad(24,16,20,12) };
        private readonly ChoiceButton category=Design.Choice(138);
        private readonly TextBox search=new TextBox { BorderStyle=BorderStyle.None, Font=Design.Font(10), AccessibleName="일기 형식 검색" };
        private readonly Label count=Design.Label("",8);
        private readonly Button add=Design.Button("이 형식 추가",true);
        public JournalTemplate SelectedTemplate { get; private set; }
        public TemplateGallery()
        {
            Text="MyDay · 일기 형식 100종"; Font=Design.Font(9); BackColor=Design.Backgrounds[0];
            AutoScaleMode=AutoScaleMode.None; StartPosition=FormStartPosition.CenterParent;
            var work=Screen.PrimaryScreen.WorkingArea;
            Size=new Size(Math.Min(Design.P(980),work.Width-32),Math.Min(Design.P(730),work.Height-32));
            MinimumSize=new Size(Math.Min(Design.P(800),work.Width-32),Math.Min(Design.P(610),work.Height-32));
            MaximizeBox=false; MinimizeBox=false;
            var header=new Panel { Dock=DockStyle.Top, Height=Design.P(144), BackColor=Color.White };
            var title=Design.Label("나에게 맞는 일기를 찾아보세요",18,true); title.Location=Design.Point(24,16); header.Controls.Add(title);
            var intro=Design.Label("100가지 형식 · 10개 분야 · 질문과 배치를 미리보고 골라요.",9); intro.ForeColor=Design.Muted; intro.Location=Design.Point(25,56); header.Controls.Add(intro);
            var filters=new FlowLayoutPanel { Location=Design.Point(24,94), Height=Design.P(36), Width=Design.P(800), WrapContents=false };
            category.Items.Add("전체 분야"); category.Items.AddRange(TemplateCatalog.CategoryNames.Cast<object>()); category.SelectedIndex=0; category.AccessibleName="일기 형식 분야";
            category.Margin=Design.Pad(0,0,12,0); category.Height=Design.P(34);
            var searchBox=new CardPanel { Size=Design.Size(300,34), Fill=Design.Backgrounds[1], Margin=Design.Pad(0,0,6,0) };
            var searchIcon=Design.Label("검색",8,true); searchIcon.ForeColor=Design.Muted; searchIcon.Location=Design.Point(12,8); searchBox.Controls.Add(searchIcon);
            search.Location=Design.Point(50,7); search.Size=Design.Size(234,22); search.BackColor=searchBox.Fill; searchBox.Controls.Add(search);
            var clear=Design.Button("×"); clear.Size=Design.Size(34,34); clear.AccessibleName="검색 지우기"; clear.Click+=delegate { search.Clear(); search.Focus(); };
            count.ForeColor=Design.Muted; count.Margin=Design.Pad(8,9,0,0);
            filters.Controls.AddRange(new Control[] {category,searchBox,clear,count}); header.Controls.Add(filters);
            var grid=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=1, Margin=Design.Pad(0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,Design.P(264))); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            grid.Controls.Add(list,0,0); grid.Controls.Add(preview,1,0);
            var footer=new Panel { Dock=DockStyle.Bottom, Height=Design.P(76), BackColor=Color.White };
            var note=Design.Label("빈 새 페이지에는 배치도 적용돼요.\n기존 글은 보존하고 새 블록을 추가해요.",8); note.ForeColor=Design.Muted; note.Location=Design.Point(24,18); footer.Controls.Add(note);
            var cancel=Design.Button("취소"); cancel.Width=Design.P(70); cancel.DialogResult=DialogResult.Cancel; footer.Controls.Add(cancel);
            add.Width=Design.P(112); add.Click+=delegate { if(SelectedTemplate!=null) { DialogResult=DialogResult.OK; Close(); } }; footer.Controls.Add(add);
            footer.Resize+=delegate { add.Location=new Point(footer.Width-Design.P(136),Design.P(20)); cancel.Location=new Point(add.Left-Design.P(82),Design.P(20)); };
            AcceptButton=add; CancelButton=cancel;
            Controls.Add(grid); Controls.Add(header); Controls.Add(footer);
            category.SelectedIndexChanged+=delegate { ApplyFilter(); };
            search.TextChanged+=delegate { ApplyFilter(); };
            search.GotFocus+=delegate { AcceptButton=null; };
            search.LostFocus+=delegate { AcceptButton=add; };
            search.KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.KeyCode==Keys.Enter) { e.Handled=true; e.SuppressKeyPress=true; } };
            KeyPreview=true; KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.Control && e.KeyCode==Keys.F) { search.Focus(); e.Handled=true; } };
            list.Resize+=delegate { ResizeOptions(); }; preview.Resize+=delegate { ResizePreview(); };
            ApplyFilter();
        }
        private void ApplyFilter()
        {
            string id=category.SelectedIndex==0?null:TemplateCatalog.Categories[category.SelectedIndex-1];
            var matches=DiaryTemplates.Find(id,search.Text); var previous=SelectedTemplate;
            list.SuspendLayout(); foreach(Control control in list.Controls.Cast<Control>().ToArray()) control.Dispose();
            foreach(var template in matches) { var item=template; var option=new TemplateOption(item); option.Click+=delegate { Select(item); }; list.Controls.Add(option); }
            list.AutoScrollPosition=Point.Empty; list.ResumeLayout(); ResizeOptions();
            count.Text=matches.Length+" / "+DiaryTemplates.All.Length+"종";
            Select(matches.Contains(previous)?previous:matches.FirstOrDefault());
        }
        private void ResizeOptions()
        {
            foreach(var button in list.Controls.OfType<TemplateOption>()) button.Width=Math.Max(Design.P(160),list.ClientSize.Width-list.Padding.Horizontal-Design.P(20));
        }
        private void Select(JournalTemplate template)
        {
            SelectedTemplate=template; add.Enabled=template!=null;
            foreach(var button in list.Controls.OfType<TemplateOption>()) { button.Active=button.Template==template; button.Invalidate(); }
            preview.SuspendLayout(); foreach(Control control in preview.Controls.Cast<Control>().ToArray()) control.Dispose();
            if(template==null) {
                var empty=Design.Label("검색 결과가 없어요.\n다른 단어나 분야로 찾아보세요.",11); empty.ForeColor=Design.Muted; preview.Controls.Add(empty);
            } else {
                var heading=Design.Label(template.Name,20,true); heading.Margin=Design.Pad(0,0,0,6); preview.Controls.Add(heading);
                var meta=Design.Label(TemplateCatalog.CategoryName(template.Category)+"  ·  약 "+template.Minutes+"분  ·  "+JournalLayouts.Name(template.Layout),8);
                meta.ForeColor=Design.Accent; meta.Margin=Design.Pad(0,0,0,12); preview.Controls.Add(meta);
                var explanation=Design.Label(template.Description,9); explanation.ForeColor=Design.Muted; explanation.Margin=Design.Pad(0,0,0,16); preview.Controls.Add(explanation);
                var diagram=new TemplateDiagram { Template=template, Size=Design.Size(480,128), Margin=Design.Pad(0,0,0,16), AccessibleName="일기 블록 배치 미리보기" }; preview.Controls.Add(diagram);
                for(int i=0;i<template.Sections.Length;i++) {
                    var section=template.Sections[i];
                    var card=new CardPanel { Fill=section.Kind=="emotion"?Design.Tint:template.Style=="paper"?Design.Paper:Color.White, Pattern=template.Style, Height=Design.P(96), Width=Design.P(480), Margin=Design.Pad(0,0,0,12) };
                    var title=Design.Label((i+1)+". "+section.Title,11,true); title.Location=Design.Point(24,20); card.Controls.Add(title);
                    var prompt=Design.Label(section.Prompt,9); prompt.ForeColor=Design.Muted; prompt.Location=Design.Point(24,55); card.Controls.Add(prompt); preview.Controls.Add(card);
                }
            }
            preview.AutoScrollPosition=Point.Empty; preview.ResumeLayout(); ResizePreview();
        }
        private void ResizePreview()
        {
            int width=Math.Max(Design.P(220),preview.ClientSize.Width-preview.Padding.Horizontal-Design.P(22));
            foreach(var control in preview.Controls.Cast<Control>()) {
                var label=control as Label;
                if(label!=null) label.MaximumSize=new Size(width,0);
                else control.Width=width;
                var card=control as CardPanel;
                if(card!=null) foreach(var text in card.Controls.OfType<Label>()) text.MaximumSize=new Size(Math.Max(Design.P(100),card.Width-Design.P(48)),0);
            }
        }
        internal void VerifyAndRender(string path)
        {
            if(list.Controls.OfType<TemplateOption>().Count()!=100) throw new Exception("Gallery did not load all 100 formats");
            foreach(var button in list.Controls.OfType<TemplateOption>()) {
                button.PerformClick();
                if(SelectedTemplate!=button.Template || preview.Controls.OfType<CardPanel>().Count()!=SelectedTemplate.Sections.Length)
                    throw new Exception("Template selection did not update preview");
            }
            for(int i=1;i<=TemplateCatalog.Categories.Length;i++) { category.SelectedIndex=i;
                if(list.Controls.OfType<TemplateOption>().Count()!=10 || list.Controls.OfType<TemplateOption>().Any(b=>b.Template.Category!=TemplateCatalog.Categories[i-1])) throw new Exception("Category filter mixed formats"); }
            category.SelectedIndex=0; search.Text="코넬";
            if(!list.Controls.OfType<TemplateOption>().Any() || list.Controls.OfType<TemplateOption>().Any(b=>!DiaryTemplates.Find(null,"코넬").Contains(b.Template))) throw new Exception("Search missed format metadata");
            category.SelectedIndex=1;
            if(SelectedTemplate!=null || add.Enabled) throw new Exception("Combined filter enabled an absent template");
            category.SelectedIndex=0; search.Text="일치하지않는검색어";
            if(add.Enabled || SelectedTemplate!=null || list.Controls.Count!=0) throw new Exception("Empty search retained stale selection");
            search.Clear(); search.Focus(); if(AcceptButton!=null) throw new Exception("Search Enter can accidentally apply a template");
            search.Text="코넬 학습"; var option=list.Controls.OfType<TemplateOption>().Single(); option.PerformClick();
            var original=Size; Size=MinimumSize; PerformLayout(); ResizePreview(); ResizeOptions();
            if(list.HorizontalScroll.Visible || preview.Controls.OfType<CardPanel>().Any(c=>c.Right>preview.ClientSize.Width)) throw new Exception("Small gallery has horizontal overflow");
            Size=original; PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(path),"windows-template-search.png"),System.Drawing.Imaging.ImageFormat.Png); }
            category.SelectedIndex=5; search.Clear(); Select(DiaryTemplates.All.First(t=>t.Id=="cornell-notes")); PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(path),"windows-template-learning.png"),System.Drawing.Imaging.ImageFormat.Png); }
            category.SelectedIndex=0; Select(DiaryTemplates.All[1]); PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(path,System.Drawing.Imaging.ImageFormat.Png); }
            add.PerformClick(); if(DialogResult!=DialogResult.OK) throw new Exception("Gallery did not confirm selection");
        }
    }
}
