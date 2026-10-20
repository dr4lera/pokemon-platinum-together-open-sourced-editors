using System.ComponentModel;
namespace InvernaMapStudio;

// Authoring metadata only; selecting a version does not enable native loading.
public sealed class GameTargetConverter:StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)=>true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)=>true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)=>new(ContentPack.Targets);
}
