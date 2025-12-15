using LinePutScript;

namespace VPet.Plugin.MathGenius
{
    public class Setting : Line
    {
        public Setting() : base("MathGenius", "")
        {
        }

        public Setting(ILine line) : base(line)
        {
        }

        public bool AutoTypeResult
        {
            get => GetBool("AutoTypeResult");
            set => SetBool("AutoTypeResult", value);
        }
    }
}
