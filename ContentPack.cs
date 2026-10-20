using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace InvernaMapStudio;

public sealed class ContentPack
{
    public string Schema {get;set;}="inverna-content/v1";
    public string Id {get;set;}="my-inverna";
    public string Name {get;set;}="My Inverna expansion";
    public string Version {get;set;}="1.0.0";
    public string Author {get;set;}="Local creator";
    public string Target {get;set;}="platinum-together-0.1.5";
    public List<Map> Maps {get;set;}=[];
    public static readonly JsonSerializerOptions Json=new(){WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
    public ContentPack Clone()=>JsonSerializer.Deserialize<ContentPack>(JsonSerializer.Serialize(this,Json),Json)!;
    public static ContentPack Create()
    {
        var p=new ContentPack();var m=new Map();p.Maps.Add(m);
        for(int y=0;y<m.Height;y++)for(int x=0;x<m.Width;x++)m.Terrain[y*m.Width+x]=(byte)(x<2||y<2||x>=m.Width-2||y>=m.Height-2?2:0);
        for(int y=0;y<m.Height;y++)for(int x=14;x<18;x++)m.Terrain[y*m.Width+x]=1;
        m.Objects.Add(new(){Id="welcome",Kind="sign",X=12,Y=14,Text="Welcome to your Inverna expansion."});
        m.Objects.Add(new(){Id="center",Kind="building",X=20,Y=8,SizeX=6,SizeY=4,Height=3,Color="#c8d1dc",RoofColor="#e76039",Text="Pokémon Center",Solid=true});
        m.Objects.Add(new(){Id="mart",Kind="building",X=5,Y=7,SizeX=5,SizeY=4,Height=2,RoofColor="#3d8ad4",Text="Poké Mart",Solid=true});
        m.Objects.Add(new(){Id="plaza-glow",Kind="effect",X=20,Y=16,Solid=false,Effect="sparkles",Color="#ffc857",Radius=2,ParticleCount=14});
        m.Objects.Add(new(){Id="street-light",Kind="lamp",X=18,Y=14,Solid=true,LightRadius=3});
        return p;
    }
    public List<string> Validate()
    {
        var errors=new List<string>();void Check(bool test,string message){if(!test)errors.Add(message);}
        Check(Schema=="inverna-content/v1","Unsupported pack schema.");
        Check(System.Text.RegularExpressions.Regex.IsMatch(Id??"","^[a-z0-9][a-z0-9-]{0,47}$"),"Pack ID must use lowercase letters, numbers and hyphens (1–48 characters).");
        Check(!string.IsNullOrWhiteSpace(Name)&&Name.Length<=100,"Pack name must have 1–100 characters.");
        Check(System.Text.RegularExpressions.Regex.IsMatch(Version??"",@"^\d{1,4}\.\d{1,4}\.\d{1,4}$"),"Version must look like 1.0.0.");
        Check(Target is "platinum-together-0.1.5" or "platinum-together-0.1.6","Unrecognized game target.");
        Check(Maps!=null&&Maps.Count is >0 and <=64,"A pack needs 1–64 maps.");
        if(Maps==null)return errors;
        var ids=new HashSet<string>();
        foreach(var m in Maps)
        {
            if(m==null){errors.Add("Null map.");continue;}
            Check(System.Text.RegularExpressions.Regex.IsMatch(m.Id??"","^[a-z0-9][a-z0-9-]{0,47}$")&&ids.Add(m.Id!),"Map IDs must be valid and unique.");
            Check(m.Width is >=8 and <=128&&m.Height is >=8 and <=128,$"{m.Id}: map dimensions must be 8–128.");
            Check(m.Terrain!=null&&m.Terrain.Length==m.Width*m.Height,$"{m.Id}: invalid terrain length.");
            Check(m.Collision!=null&&m.Collision.Length==m.Width*m.Height,$"{m.Id}: invalid collision length.");
            if(m.Terrain!=null)Check(m.Terrain.All(t=>t<=5),$"{m.Id}: unknown terrain ID.");
            if(m.Collision!=null)Check(m.Collision.All(t=>t<=1),$"{m.Id}: invalid collision value.");
            Check(m.InBounds(m.SpawnX,m.SpawnY),$"{m.Id}: spawn is outside the map.");
            if(m.Collision?.Length==m.Width*m.Height&&m.InBounds(m.SpawnX,m.SpawnY))Check(m.Collision[m.SpawnY*m.Width+m.SpawnX]==0,$"{m.Id}: spawn is blocked.");
            Check(m.Objects!=null&&m.Objects.Count<=2048,$"{m.Id}: too many objects.");
            if(m.Objects==null)continue;
            Check(!m.Objects.Any(o=>o!=null&&o.Visible&&o.Solid&&o.Kind!="warp"&&o.Contains(m.SpawnX,m.SpawnY)),$"{m.Id}: spawn is inside a solid object.");
            Check(m.Environment!=null&&m.Environment.Hour is >=0 and <=23&&m.Environment.Ambient is >=0 and <=100&&m.Environment.Fog is >=0 and <=100,$"{m.Id}: invalid environment.");
            var objectIds=new HashSet<string>();
            foreach(var o in m.Objects)
            {
                if(o==null){errors.Add($"{m.Id}: null object.");continue;}
                Check(!string.IsNullOrWhiteSpace(o.Id)&&o.Id.Length<=80&&objectIds.Add(o.Id),$"{m.Id}: duplicate or invalid object ID.");
                Check(MapObject.Kinds.Contains(o.Kind),$"{m.Id}/{o.Id}: unknown object kind.");
                Check(m.InBounds(o.X,o.Y),$"{m.Id}/{o.Id}: object outside map.");
                Check(o.Elevation is >=-32 and <=64&&o.Rotation is >=0 and <360,$"{m.Id}/{o.Id}: elevation/rotation out of range.");
                Check(o.NativeModelId is >=-1 and <=65535,$"{m.Id}/{o.Id}: invalid native model reference.");
                Check(o.SizeX is >=1 and <=32&&o.SizeY is >=1 and <=32&&o.Height is >=0 and <=32&&m.InBounds(o.X+o.SizeX-1,o.Y+o.SizeY-1),$"{m.Id}/{o.Id}: invalid object footprint or height.");
                Check(o.Opacity is >=0 and <=100&&o.Radius is >=0 and <=32&&o.ParticleCount is >=0 and <=256&&o.LightRadius is >=0 and <=32&&o.Speed is >=0 and <=10,$"{m.Id}/{o.Id}: invalid effect settings.");
                Check(MapObject.Effects.Contains(o.Effect)&&MapObject.ValidColor(o.Color)&&MapObject.ValidColor(o.RoofColor),$"{m.Id}/{o.Id}: invalid effect/color.");
                Check(o.Text!=null&&o.Text.Length<=4000,$"{m.Id}/{o.Id}: dialogue too long.");
                if(o.Kind=="warp"){
                    var dest=Maps.FirstOrDefault(x=>x?.Id==o.Destination);
                    Check(dest!=null&&dest.InBounds(o.DestinationX,o.DestinationY),$"{m.Id}/{o.Id}: invalid warp destination.");
                    if(dest?.Collision?.Length==dest?.Width*dest?.Height&&dest!=null&&dest.InBounds(o.DestinationX,o.DestinationY))Check(dest.Collision[o.DestinationY*dest.Width+o.DestinationX]==0,$"{m.Id}/{o.Id}: warp destination blocked.");
                    if(dest?.Objects!=null)Check(!dest.Objects.Any(ob=>ob!=null&&ob.Visible&&ob.Solid&&ob.Kind!="warp"&&ob.Contains(o.DestinationX,o.DestinationY)),$"{m.Id}/{o.Id}: warp destination is inside a solid object.");
                }
            }
        }
        return errors;
    }
    public void RequireValid(){var errors=Validate();if(errors.Count>0)throw new InvalidDataException(string.Join("\n",errors.Take(30)));}
    static string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
    public void SaveProject(string path){RequireValid();AtomicWrite(path,JsonSerializer.SerializeToUtf8Bytes(this,Json));}
    public static ContentPack LoadProject(string path){if(new FileInfo(path).Length>16*1024*1024)throw new InvalidDataException("Project too large.");var p=JsonSerializer.Deserialize<ContentPack>(File.ReadAllBytes(path),Json)??throw new InvalidDataException("Empty project.");p.RequireValid();return p;}
    public void Export(string path)
    {
        RequireValid();var project=JsonSerializer.SerializeToUtf8Bytes(this,Json);
        var manifest=JsonSerializer.SerializeToUtf8Bytes(new {schema="inverna-pack/v1",id=Id,name=Name,version=Version,target=Target,runtime="adapter-required",files=new Dictionary<string,string>{{"project.json",Hash(project)}}},Json);
        var tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{using(var zip=ZipFile.Open(tmp,ZipArchiveMode.Create)){using(var s=zip.CreateEntry("manifest.json").Open())s.Write(manifest);using(var s=zip.CreateEntry("project.json").Open())s.Write(project);}File.Move(tmp,path,true);}finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
    public static ContentPack Import(string path)
    {
        if(new FileInfo(path).Length>16*1024*1024)throw new InvalidDataException("Pack too large.");
        using var zip=ZipFile.OpenRead(path);
        if(zip.Entries.Count!=2||zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=2||zip.Entries.Any(e=>e.FullName is not ("manifest.json" or "project.json")||e.Length>16*1024*1024))throw new InvalidDataException("Unexpected pack entries.");
        static byte[] Read(ZipArchiveEntry e,int max){if(e.Length>max)throw new InvalidDataException("Entry too large.");using var s=e.Open();using var m=new MemoryStream();var buf=new byte[8192];int n;while((n=s.Read(buf))>0){if(m.Length+n>max)throw new InvalidDataException("Entry too large.");m.Write(buf,0,n);}return m.ToArray();}
        using var manifest=JsonDocument.Parse(Read(zip.GetEntry("manifest.json")!,65536));var root=manifest.RootElement;
        var bytes=Read(zip.GetEntry("project.json")!,16*1024*1024);
        if(root.GetProperty("schema").GetString()!="inverna-pack/v1"||root.GetProperty("files").GetProperty("project.json").GetString()!=Hash(bytes))throw new InvalidDataException("Pack integrity check failed.");
        var p=JsonSerializer.Deserialize<ContentPack>(bytes,Json)??throw new InvalidDataException("Empty project.");p.RequireValid();
        if(root.GetProperty("id").GetString()!=p.Id||root.GetProperty("version").GetString()!=p.Version||root.GetProperty("target").GetString()!=p.Target||root.GetProperty("runtime").GetString()!="adapter-required")throw new InvalidDataException("Manifest disagrees with project.");return p;
    }
    static void AtomicWrite(string path,byte[] bytes){var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,bytes);File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
}
public sealed class Map
{
    public string Id {get;set;}="plaza";public string Name {get;set;}="New plaza";
    public int Width {get;set;}=32;public int Height {get;set;}=32;
    public byte[] Terrain {get;set;}=new byte[1024];public byte[] Collision {get;set;}=new byte[1024];
    public int SpawnX {get;set;}=16;public int SpawnY {get;set;}=22;
    public List<MapObject> Objects {get;set;}=[];
    public MapEnvironment Environment {get;set;}=new();
    public bool Blocked(int x,int y)=>!InBounds(x,y)||Collision[y*Width+x]!=0||Objects.Any(o=>o.Visible&&o.Solid&&o.Kind!="warp"&&o.Contains(x,y));
    public bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Width&&y<Height;
    public override string ToString()=>Name;
}
public sealed class MapObject
{
    public static readonly string[] Kinds=["bench","lamp","planter","sign","building","npc","warp","effect"];
    public string Id {get;set;}="object";public string Kind {get;set;}="bench";
    public int X {get;set;}public int Y {get;set;}
    public int Elevation {get;set;}public int Rotation {get;set;}
    public bool Float {get;set;}public bool Solid {get;set;}=true;
    public int NativeModelId {get;set;}=-1;
    public static readonly string[] Effects=["sparkles","smoke","rain","snow","fire","glow","ring","none"];
    public int SizeX {get;set;}=1;public int SizeY {get;set;}=1;public int Height {get;set;}=1;
    public string Color {get;set;}="#a9b9cc";public string RoofColor {get;set;}="#d67d35";
    public bool Visible {get;set;}=true;public int Opacity {get;set;}=100;
    public string Effect {get;set;}="sparkles";public int Radius {get;set;}=1;public int ParticleCount {get;set;}=12;public double Speed {get;set;}=1;
    public int LightRadius {get;set;}public bool Enabled {get;set;}=true;
    public string Text {get;set;}="";
    public string Destination {get;set;}="";public int DestinationX {get;set;}public int DestinationY {get;set;}
    public bool Contains(int x,int y)=>x>=X&&y>=Y&&x<X+SizeX&&y<Y+SizeY;
    public static bool ValidColor(string? value)=>value!=null&&System.Text.RegularExpressions.Regex.IsMatch(value,"^#[0-9a-fA-F]{6}$");
    public override string ToString()=>$"{Id} ({Kind})";
}
[System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
public sealed class MapEnvironment
{
    public int Hour {get;set;}=14;public int Ambient {get;set;}=90;public int Fog {get;set;}=0;
    public string Weather {get;set;}="clear";public string Music {get;set;}="";public string SkyColor {get;set;}="#364b65";
    public override string ToString()=>$"{Hour:00}:00 · {Weather}";
}
