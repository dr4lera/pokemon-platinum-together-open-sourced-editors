using System.IO.Compression;
using System.Text.Json;
namespace InvernaMapStudio;
static class CreatorChecks
{
    public static void Run()
    {
        void Check(bool b,string reason){if(!b)throw new Exception(reason);}
        var pixels=new string?[1024];pixels[495]="#123456";var bytes=JsonSerializer.SerializeToUtf8Bytes(new{format="pt-creator-draft-preview",version=3,name="Synthetic creator test",frames=new Dictionary<string,object>{{"walk-run/4",new{size=new[]{32,32},pixels}}},layers=new[]{new{id="artwork",visible=true}},undo=Array.Empty<object>()});
        var p=CreatorProject.Parse(bytes);Check(p.Frames.Length==1,"Creator frame lost");Check(p.Frames[0].Pixels[495].ToArgb()==Color.FromArgb(0x12,0x34,0x56).ToArgb(),"Creator color changed");
        using(var sheet=p.Sheet("walk-run")){Check(sheet.Width==256&&sheet.Height==128,"Pose sheet slot layout incorrect");Check(sheet.GetPixel(4*32+15,15).ToArgb()==Color.FromArgb(0x12,0x34,0x56).ToArgb(),"Sprite slot moved");Check(sheet.GetPixel(0,0).A==0,"Empty sprite frame became opaque");}
        var dir=Path.Combine(Path.GetTempPath(),"creator-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{
            var copy=Path.Combine(dir,"copy.ptdraft.json");p.SaveCopy(copy);Check(File.ReadAllBytes(copy).SequenceEqual(bytes),"Layers/history project copy changed");
            var pack=Path.Combine(dir,"art.pttrainer");p.ExportPack(pack);using var zip=ZipFile.OpenRead(pack);Check(zip.Entries.Count==3,"Unexpected art pack contents");using var manifest=JsonDocument.Parse(zip.GetEntry("manifest.json")!.Open());var sheet=manifest.RootElement.GetProperty("sheets")[0];Check(sheet.GetProperty("columns").GetInt32()==8&&sheet.GetProperty("frameCount").GetInt32()==32,"Manifest pose layout disagrees with PNG");
            var invalid=JsonSerializer.SerializeToUtf8Bytes(new{format="pt-creator-draft-preview",version=3,name="Bad",frames=new Dictionary<string,object>{{"../../escape",new{size=new[]{1,1},pixels=new[]{"#000000"}}}}});bool refused=false;try{CreatorProject.Parse(invalid);}catch(InvalidDataException){refused=true;}Check(refused,"Unsafe sprite path accepted");
            invalid=JsonSerializer.SerializeToUtf8Bytes(new{format="pt-creator-draft-preview",version=3,name="Bad",frames=new Dictionary<string,object>{{"walk-run/0",new{size=new[]{32,32},pixels=new[]{"#000000"}}}}});refused=false;try{CreatorProject.Parse(invalid);}catch(InvalidDataException){refused=true;}Check(refused,"Wrong pixel count accepted");
        }finally{Directory.Delete(dir,true);}
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"creator-selftest.json"),JsonSerializer.Serialize(new{passed=true,checks=new[]{"v3 import","color preservation","pose slot layout","transparent missing frames","project bytes preserved","pack manifest dimensions","unsafe key rejection","pixel count rejection"}}));
    }
}
