namespace InvernaMapStudio;
internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        if(args.Contains("--selftest")){try{SelfTest.Run();CreatorChecks.Run();AnimationTime.SelfTest();Environment.ExitCode=0;}catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"selftest-error.txt"),e.ToString());Environment.ExitCode=1;}return;}
        if(args.Length==2&&args[0]=="--check-creator"){var p=CreatorProject.Load(args[1]);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"creator-import-check.json"),System.Text.Json.JsonSerializer.Serialize(new{p.Name,frames=p.Frames.Length,originalPreserved=p.Original.SequenceEqual(File.ReadAllBytes(args[1]))}));return;}
        ApplicationConfiguration.Initialize();Application.Run(args.Contains("--sprites")?(Form)new SpriteStudio():new Editor());
    }
}
