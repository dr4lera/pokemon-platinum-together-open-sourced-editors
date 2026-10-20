using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace InvernaMapStudio;
public sealed class SpriteStudio:Form
{
    CreatorProject? project;readonly ListBox frames=new(){Dock=DockStyle.Fill};readonly SpriteCanvas canvas=new(){Dock=DockStyle.Fill};
    readonly Label status=new(){Dock=DockStyle.Bottom,Height=64,Padding=new(12),Text="Import your .ptdraft.json from the official creator. Your original project is preserved."};
    readonly System.Windows.Forms.Timer timer=new(){Interval=140};CreatorFrame[] animation=[];int animationIndex;
    public SpriteStudio()
    {
        AutoScaleMode=AutoScaleMode.None;Text="Plat Together · Trainer Sprite Studio";ClientSize=new(1020,720);MinimumSize=new(800,580);Font=new("Segoe UI",10);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=90,Padding=new(8),WrapContents=true};
        void Button(string title,Action action){var b=new Button{Text=title,AutoSize=true,Height=35};b.Click+=(_,_)=>{try{action();}catch(Exception e){MessageBox.Show(this,e.Message,"Trainer sprites",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};bar.Controls.Add(b);}
        Button("Open official creator",()=>Process.Start(new ProcessStartInfo("https://platinumtogether.online/creators/"){UseShellExecute=true}));
        Button("Import project",()=>{using var d=new OpenFileDialog{Filter="Creator project|*.ptdraft.json;*.json"};if(d.ShowDialog(this)!=DialogResult.OK)return;var loaded=CreatorProject.Load(d.FileName);timer.Stop();project=loaded;frames.Items.Clear();frames.Items.AddRange(loaded.Frames.Cast<object>().ToArray());if(frames.Items.Count>0)frames.SelectedIndex=0;else canvas.Frame=null;canvas.Invalidate();status.Text=loaded.Name+" · "+loaded.Frames.Length+" saved frames. Native game installation needs a separate adapter.";});
        Button("Back up project",()=>{var p=Require();using var d=new SaveFileDialog{Filter="Creator project|*.ptdraft.json",FileName="trainer-backup.ptdraft.json"};if(d.ShowDialog(this)==DialogResult.OK)p.SaveCopy(d.FileName);});
        Button("Export frame PNG",()=>{if(frames.SelectedItem is not CreatorFrame f)throw new IOException("Select a frame first.");using var d=new SaveFileDialog{Filter="PNG|*.png",FileName=f.Family+"-"+(f.Index+1)+".png"};if(d.ShowDialog(this)==DialogResult.OK){using var image=f.Bitmap();image.Save(d.FileName,System.Drawing.Imaging.ImageFormat.Png);}});
        Button("Export pose sheet",()=>{if(frames.SelectedItem is not CreatorFrame f)throw new IOException("Select a frame first.");using var d=new SaveFileDialog{Filter="PNG|*.png",FileName=f.Family+".png"};if(d.ShowDialog(this)==DialogResult.OK){using var sheet=Require().Sheet(f.Family);sheet.Save(d.FileName,System.Drawing.Imaging.ImageFormat.Png);}});
        Button("Export art pack",()=>{var p=Require();using var d=new SaveFileDialog{Filter="Trainer art pack|*.pttrainer",FileName="my-trainer.pttrainer"};if(d.ShowDialog(this)==DialogResult.OK)p.ExportPack(d.FileName);});
        Button("Play / stop pose",()=>{if(timer.Enabled){timer.Stop();return;}if(frames.SelectedItem is not CreatorFrame f)throw new IOException("Select a frame first.");var group=Require().Frames.Where(x=>x.Family==f.Family).ToArray();animation=new CreatorFrame[CreatorProject.FrameCount(f.Family,group.Max(x=>x.Index)+1)];foreach(var x in group)animation[x.Index]=x;animationIndex=0;timer.Start();});
        var split=new SplitContainer{Size=new(1020,566),Dock=DockStyle.Fill,SplitterDistance=310};split.Panel1.Controls.Add(frames);split.Panel2.Controls.Add(canvas);Controls.Add(split);Controls.Add(status);Controls.Add(bar);split.BringToFront();
        frames.SelectedIndexChanged+=(_,_)=>{timer.Stop();canvas.Frame=frames.SelectedItem as CreatorFrame;canvas.Invalidate();};timer.Tick+=(_,_)=>{if(animation.Length==0)return;canvas.Frame=animation[animationIndex++%animation.Length];canvas.Invalidate();};
        AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);PerformAutoScale();Shown+=(_,_)=>split.SplitterDistance=Math.Min((int)Math.Round(310*DeviceDpi/96d),split.Width/3);
    }
    CreatorProject Require()=>project??throw new IOException("Import a creator project first.");
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    sealed class SpriteCanvas:Control
    {
        public CreatorFrame? Frame;public SpriteCanvas(){DoubleBuffered=true;BackColor=Color.FromArgb(26,39,52);}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Frame==null)return;int scale=Math.Max(1,Math.Min(12,Math.Min((Width-32)/Frame.Width,(Height-32)/Frame.Height)));int w=Frame.Width*scale,h=Frame.Height*scale,x=(Width-w)/2,y=(Height-h)/2;
            for(int row=0;row<Frame.Height;row++)for(int col=0;col<Frame.Width;col++){using var brush=new SolidBrush((row+col)%2==0?Color.FromArgb(91,103,115):Color.FromArgb(119,131,142));e.Graphics.FillRectangle(brush,x+col*scale,y+row*scale,scale,scale);}
            using var image=Frame.Bitmap();e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(image,new Rectangle(x,y,w,h),0,0,Frame.Width,Frame.Height,GraphicsUnit.Pixel);
        }
    }
}
