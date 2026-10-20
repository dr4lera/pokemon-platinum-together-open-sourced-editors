using System.Drawing.Drawing2D;
namespace InvernaMapStudio;
public sealed partial class Editor
{
    readonly ComboBox scene=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=230};
    void SceneTools(FlowLayoutPanel bar)
    {
        bar.Controls.Add(new Label{Text="Scene:",AutoSize=true,Padding=new(0,7,0,0)});bar.Controls.Add(scene);scene.SelectedIndexChanged+=(_,_)=>{if(refreshing)return;canvas.Selected=scene.SelectedItem as MapObject;properties.SelectedObject=canvas.Selected;canvas.Invalidate();};
        var environment=new Button{Text="Lighting / weather / audio",AutoSize=true};bar.Controls.Add(environment);environment.Click+=(_,_)=>properties.SelectedObject=current.Environment;
        var duplicate=new Button{Text="Duplicate object",AutoSize=true};bar.Controls.Add(duplicate);duplicate.Click+=(_,_)=>Guard(()=>{if(canvas.Selected==null)return;PushUndo();var copy=System.Text.Json.JsonSerializer.Deserialize<MapObject>(System.Text.Json.JsonSerializer.Serialize(canvas.Selected))!;copy.Id+="-"+Guid.NewGuid().ToString("N")[..6];copy.X=Math.Min(current.Width-copy.SizeX,copy.X+1);current.Objects.Add(copy);canvas.Selected=copy;properties.SelectedObject=copy;Changed();});
    }
    void RefreshScene(){var selection=canvas.Selected;scene.Items.Clear();scene.Items.AddRange(current.Objects.Cast<object>().ToArray());scene.SelectedItem=selection;}
    sealed partial class MapCanvas
    {
        readonly System.Windows.Forms.Timer animation=new(){Interval=50};double seconds;
        void StartAnimation(){animation.Tick+=(_,_)=>{seconds+=.05;Invalidate();};animation.Start();}
        protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
        static Color Parse(string s)=>MapObject.ValidColor(s)?ColorTranslator.FromHtml(s):Color.SlateGray;
        void DrawObjects(Graphics g)
        {
            foreach(var o in owner.current.Objects.Where(o=>o.Visible).OrderBy(o=>o.Y+o.SizeY).ThenBy(o=>o.Elevation))
            {
                float x=origin.X+o.X*size,y=origin.Y+o.Y*size-o.Elevation*size*.35f,w=o.SizeX*size,h=o.SizeY*size;
                if(o.Float)y-=(float)Math.Sin(seconds*2)*size*.12f+size*.3f;
                using var fill=new SolidBrush(Color.FromArgb(Math.Clamp(o.Opacity*255/100,0,255),Parse(o.Color)));using var roof=new SolidBrush(Parse(o.RoofColor));
                var saved=g.Save();g.TranslateTransform(x+w/2,y+h/2);g.RotateTransform(o.Rotation);g.TranslateTransform(-w/2,-h/2);
                if(o.Kind=="building")
                {
                    float lift=o.Height*size*.65f;g.FillRectangle(Brushes.Black,5,5,w,h);g.FillRectangle(fill,0,-lift,w,h+lift);g.DrawRectangle(Pens.DarkSlateGray,0,-lift,w,h+lift);
                    g.FillPolygon(roof,new PointF[]{new(0,-lift),new(w/2,-lift-size*.6f),new(w,-lift),new(w,h*.65f-lift),new(0,h*.65f-lift)});g.DrawLine(Pens.SaddleBrown,0,h*.65f-lift,w,h*.65f-lift);
                    g.FillRectangle(Brushes.MidnightBlue,w*.42f,h-size,w*.17f,size);for(int i=0;i<Math.Max(1,o.SizeX/2);i++)g.FillRectangle(Brushes.LightSkyBlue,size*.25f+i*size*1.6f,h-size*1.8f,size*.6f,size*.55f);
                    if(size>=16)g.DrawString(string.IsNullOrWhiteSpace(o.Text)?o.Id:o.Text,Font,Brushes.White,2,-lift);
                }
                else if(o.Kind=="bench"){g.FillRectangle(roof,0,h*.2f,w,h*.45f);g.DrawLine(Pens.Black,w*.1f,h*.7f,w*.1f,h);g.DrawLine(Pens.Black,w*.9f,h*.7f,w*.9f,h);}
                else if(o.Kind=="planter"){g.FillRectangle(fill,0,h*.35f,w,h*.65f);g.FillEllipse(Brushes.ForestGreen,0,-h*.1f,w,h*.9f);}
                else if(o.Kind=="lamp"){g.FillRectangle(Brushes.DimGray,w*.45f,-size,w*.12f,h+size);g.FillEllipse(Brushes.Gold,w*.15f,-size*1.2f,w*.7f,size*.6f);}
                else if(o.Kind=="effect"){}
                else if(o.Kind=="npc"){g.FillEllipse(Brushes.Coral,w*.2f,-size*.2f,w*.6f,h*.8f);g.FillRectangle(fill,w*.2f,h*.45f,w*.6f,h*.55f);}
                else if(o.Kind=="warp"){g.DrawEllipse(Pens.MediumPurple,0,0,w,h);g.DrawEllipse(Pens.MediumPurple,w*.2f,h*.2f,w*.6f,h*.6f);}
                else {g.FillRectangle(roof,0,0,w,h*.6f);g.FillRectangle(Brushes.SaddleBrown,w*.45f,h*.5f,w*.1f,h*.5f);}
                g.Restore(saved);
                if(o.Enabled&&(o.Kind=="effect"||o.LightRadius>0))DrawEffect(g,o,x+w/2,y+h/2);
                if(o==Selected){g.DrawRectangle(Pens.Yellow,x,y,w,h);g.DrawString(o.Id,Font,Brushes.Yellow,x,y+h);}
            }
            var env=owner.current.Environment;
            if(env!=null){var darkness=Math.Clamp(100-env.Ambient+(env.Hour<6||env.Hour>=20?25:0),0,85);using var tint=new SolidBrush(Color.FromArgb(darkness*2,14,21,48));g.FillRectangle(tint,0,0,Width,Height);if(env.Fog>0){using var fog=new SolidBrush(Color.FromArgb(Math.Clamp(env.Fog*2,0,220),200,212,220));g.FillRectangle(fog,0,0,Width,Height);}}
        }
        void DrawEffect(Graphics g,MapObject o,float x,float y)
        {
            if(o.Effect=="none"&&o.LightRadius==0)return;var c=Parse(o.Color);float radius=Math.Max(o.Radius,o.LightRadius)*size;
            using var pen=new Pen(Color.FromArgb(170,c),1.5f);using var brush=new SolidBrush(Color.FromArgb(180,c));
            if(o.LightRadius>0||o.Effect=="glow"){for(int k=5;k>=1;k--){float r=radius*k/5;using var glow=new SolidBrush(Color.FromArgb(12,c));g.FillEllipse(glow,x-r,y-r,r*2,r*2);}}
            if(o.Effect=="ring"){float r=radius*(.7f+.15f*(float)Math.Sin(seconds*o.Speed));g.DrawEllipse(pen,x-r,y-r,r*2,r*2);return;}
            for(int i=0;i<o.ParticleCount&&o.Kind=="effect";i++){double t=seconds*o.Speed+i*.6180339887,a=i*2.39996;float px=x+(float)Math.Cos(a)*radius*(.2f+(i%7)/9f),py=y+(float)Math.Sin(a)*radius-(float)(t%1)*size;float r=2+(float)Math.Sin(t*4)*1.2f;if(o.Effect is "rain")g.DrawLine(pen,px,py,px-3,py+10);else if(o.Effect is "smoke" or "fire"){using var smoke=new SolidBrush(Color.FromArgb(80,o.Effect=="fire"?Color.OrangeRed:c));g.FillEllipse(smoke,px,py,r*4,r*4);}else g.FillEllipse(brush,px,py,r,r);}
        }
    }
}
