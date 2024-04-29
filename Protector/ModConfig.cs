using System;

namespace Protector
{
    public sealed class ModConfig
    {
        public bool IsAutoActive { get; set; }        // is protector active
        public string BindKey { get; set; }       // toggle key
        public float Speed { get; set; }            // speed
        public int Range { get; set; }            // detected range
        public bool Explode { get; set; }         // whether ammo explode

        public ModConfig()
        {
            // default setting
            this.IsAutoActive = false;
            this.BindKey = "F6";
            this.Speed = 30;
            this.Range = 300;
            this.Explode = false;
        }

        public ModConfig(bool isAutoActive, string bindKey, float speed, int range, bool explode)
        {
            IsAutoActive = isAutoActive;
            BindKey = bindKey;
            Speed = speed;
            Range = range;
            Explode = explode;
        }
    }
}
