using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace InvernaMapStudio;

public sealed record CreatorFrame(string Family,int Index,int Width,int Height,Color[] Pixels)
{
    public override string ToString()=>$"{Family} · frame {Index+1} · {Width} × {Height}";
    public Bitmap Bitmap(){var image=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)image.SetPixel(x,y,Pixels[y*Width+x]);return image;}
}
public sealed class CreatorProject
{
    public string Name {get;private init;}="";
    public byte[] Original {get;private init;}=[];
    public CreatorFrame[] Frames {get;private init;}=[];
    public static int FrameCount(string family,int minimum)=>family switch{"walk-run"=>Math.Max(32,minimum),"bike"=>Math.Max(24,minimum),"fishing"=>Math.Max(16,minimum),"surf"=>Math.Max(4,minimum),"rear"=>Math.Max(8,minimum),_=>minimum};
    public static CreatorProject Load(string path){if(new FileInfo(path).Length>32*1024*1024)throw new IOException("Creator project exceeds 32 MiB.");return Parse(File.ReadAllBytes(path));}
    public static CreatorProject Parse(byte[] bytes)
    {
        if(bytes.Length>32*1024*1024)throw new IOException("Creator project exceeds 32 MiB.");
        using var document=JsonDocument.Parse(bytes,new JsonDocumentOptions{MaxDepth=48});var p=document.RootElement;
        if(p.GetProperty("format").GetString()!="pt-creator-draft-preview"||p.GetProperty("version").GetInt32()!=3)throw new InvalidDataException("Choose a version 3 .ptdraft.json export from the official Sprite Creator.");
        var name=p.GetProperty("name").GetString()??"My trainer";if(name.Length>200)throw new InvalidDataException("Trainer name is too long.");
        var frames=new List<CreatorFrame>();var keys=new HashSet<string>();long total=0;
        foreach(var property in p.GetProperty("frames").EnumerateObject()){
            if(!keys.Add(property.Name)||!Regex.IsMatch(property.Name,@"^[a-z][a-z0-9-]{0,47}/[0-9]{1,3}$"))throw new InvalidDataException("Invalid or duplicate frame key.");
            if(frames.Count>=1024)throw new InvalidDataException("Too many frames.");
            var parts=property.Name.Split('/');int index=int.Parse(parts[1],CultureInfo.InvariantCulture);if(index>=512)throw new InvalidDataException("Frame index is out of range.");
            var f=property.Value;var size=f.GetProperty("size");if(size.GetArrayLength()!=2)throw new InvalidDataException("Invalid frame dimensions.");int w=size[0].GetInt32(),h=size[1].GetInt32();
            if(w is <1 or >256||h is <1 or >256||(total+=(long)w*h)>4_000_000)throw new InvalidDataException("Frame dimensions or total pixel budget exceeded.");
            var values=f.GetProperty("pixels");if(values.GetArrayLength()!=w*h)throw new InvalidDataException("Frame pixel count does not match its dimensions.");
            var pixels=new Color[w*h];int i=0;foreach(var value in values.EnumerateArray()){
                if(value.ValueKind==JsonValueKind.Null){pixels[i++]=Color.Transparent;continue;}
                var hex=value.GetString();if(hex==null||!Regex.IsMatch(hex,"^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$"))throw new InvalidDataException("Unsupported frame color; expected #RRGGBB or #RRGGBBAA.");
                int r=int.Parse(hex.AsSpan(1,2),NumberStyles.HexNumber),g=int.Parse(hex.AsSpan(3,2),NumberStyles.HexNumber),b=int.Parse(hex.AsSpan(5,2),NumberStyles.HexNumber),a=hex.Length==9?int.Parse(hex.AsSpan(7,2),NumberStyles.HexNumber):255;
                pixels[i++]=Color.FromArgb(a,r,g,b);
            }
            frames.Add(new(parts[0],index,w,h,pixels));
        }
        foreach(var group in frames.GroupBy(f=>f.Family))if(group.Select(f=>(f.Width,f.Height)).Distinct().Count()!=1)throw new InvalidDataException("A pose group has inconsistent frame sizes.");
        return new(){Name=name,Original=(byte[])bytes.Clone(),Frames=frames.OrderBy(f=>f.Family,StringComparer.Ordinal).ThenBy(f=>f.Index).ToArray()};
    }
    public void SaveCopy(string path){var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,Original);if(!File.ReadAllBytes(temp).SequenceEqual(Original))throw new IOException("Project copy verification failed.");File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
    public Bitmap Sheet(string family)
    {
        var frames=Frames.Where(f=>f.Family==family).ToArray();if(frames.Length==0)throw new IOException("This pose group has no saved artwork.");
        int count=FrameCount(family,frames.Max(f=>f.Index)+1);
        var first=frames[0];int columns=Math.Min(8,count),rows=(count+columns-1)/columns;
        if((long)columns*first.Width*rows*first.Height>4_000_000)throw new IOException("Sprite sheet is too large.");
        var sheet=new Bitmap(columns*first.Width,rows*first.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g=Graphics.FromImage(sheet);g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        foreach(var f in frames){using var image=f.Bitmap();g.DrawImageUnscaled(image,f.Index%columns*f.Width,f.Index/columns*f.Height);}return sheet;
    }
    public void ExportPack(string path)
    {
        if(Frames.Length==0)throw new IOException("Draw/import at least one frame before exporting.");
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{
            using(var zip=ZipFile.Open(temp,ZipArchiveMode.Create)){
                var entries=new List<object>();
                foreach(var group in Frames.GroupBy(f=>f.Family)){
                    var file="sheets/"+group.Key+".png";using var bitmap=Sheet(group.Key);using var memory=new MemoryStream();bitmap.Save(memory,System.Drawing.Imaging.ImageFormat.Png);var data=memory.ToArray();using(var output=zip.CreateEntry(file).Open())output.Write(data);
                    int frameCount=FrameCount(group.Key,group.Max(f=>f.Index)+1);
                    entries.Add(new{family=group.Key,path=file,width=group.First().Width,height=group.First().Height,columns=Math.Min(8,frameCount),frameCount,drawn=group.Select(f=>f.Index).ToArray(),sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data)).ToLowerInvariant()});
                }
                using(var output=zip.CreateEntry("creator.ptdraft.json").Open())output.Write(Original);
                using var manifest=zip.CreateEntry("manifest.json").Open();JsonSerializer.Serialize(manifest,new{schema="plat-trainer-art/v1",name=Name,runtime="adapter-required",source="https://platinumtogether.online/creators/",sheets=entries},new JsonSerializerOptions{WriteIndented=true});
            }File.Move(temp,path,true);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
