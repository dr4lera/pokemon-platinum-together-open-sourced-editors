using System.IO.Compression;
using System.Text.Json;
namespace InvernaMapStudio;
internal static class SelfTest
{
    public static void Run()
    {
        var dir=Path.Combine(Path.GetTempPath(),"InvernaStudio-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        void Assert(bool condition,string why){if(!condition)throw new Exception(why);}
        try{
            var p=ContentPack.Create();p.RequireValid();p.Maps[0].Collision[0]=1;p.Maps[0].Objects.Add(new(){Id="npc",Kind="npc",X=4,Y=5,Text="Testing dialogue",Solid=true});
            p.SaveProject(Path.Combine(dir,"project.json"));var loaded=ContentPack.LoadProject(Path.Combine(dir,"project.json"));Assert(loaded.Maps[0].Collision[0]==1,"Collision lost.");
            var path=Path.Combine(dir,"pack.ptpack");p.Export(path);var restored=ContentPack.Import(path);Assert(JsonSerializer.Serialize(p,ContentPack.Json)==JsonSerializer.Serialize(restored,ContentPack.Json),"Pack round trip changed content.");
            var bad=p.Clone();bad.Maps[0].SpawnX=-1;Assert(bad.Validate().Count>0,"Invalid spawn accepted.");bad=p.Clone();bad.Maps[0].Collision[bad.Maps[0].SpawnY*32+bad.Maps[0].SpawnX]=1;Assert(bad.Validate().Count>0,"Blocked spawn accepted.");bad=p.Clone();bad.Maps[0].Objects.Add(new(){Id="exit",Kind="warp",X=2,Y=2,Destination="absent"});Assert(bad.Validate().Count>0,"Broken warp accepted.");
            bad=p.Clone();bad.Maps[0].Objects.Add(new(){Id="spawn-block",Kind="building",X=15,Y=21,SizeX=3,SizeY=3});Assert(bad.Validate().Any(e=>e.Contains("spawn is inside")),"Solid footprint at spawn accepted.");
            bad=p.Clone();bad.Maps[0].Objects.Add(new(){Id="exit",Kind="warp",X=2,Y=2,Destination=bad.Maps[0].Id,DestinationX=20,DestinationY=8});Assert(bad.Validate().Any(e=>e.Contains("warp destination is inside")),"Warp into building accepted.");
            bad=p.Clone();bad.Maps[0].Objects[0].SizeX=33;Assert(bad.Validate().Count>0,"Oversized object accepted.");
            bad=p.Clone();bad.Target="platinum-together-0.1.6";bad.RequireValid();
            using(var zip=ZipFile.Open(path,ZipArchiveMode.Update)){zip.GetEntry("project.json")!.Delete();using var w=new StreamWriter(zip.CreateEntry("project.json").Open());w.Write("{}");}
            bool rejected=false;try{ContentPack.Import(path);}catch(InvalidDataException){rejected=true;}Assert(rejected,"Tampered pack accepted.");
            p.Export(path);using(var zip=ZipFile.Open(path,ZipArchiveMode.Update)){zip.CreateEntry("../escape.txt");}rejected=false;try{ContentPack.Import(path);}catch(InvalidDataException){rejected=true;}Assert(rejected,"Traversal entry accepted.");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"selftest.json"),JsonSerializer.Serialize(new{passed=true,checks=new[]{"project roundtrip","pack roundtrip","collision persistence","spawn bounds","blocked spawn","warp links","solid spawn footprint","solid warp destination","object size limits","0.1.6 authoring target","tamper rejection","path traversal rejection"}},ContentPack.Json));
        }finally{Directory.Delete(dir,true);}
    }
}
