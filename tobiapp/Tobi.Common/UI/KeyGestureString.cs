using System.ComponentModel;
using System.Configuration;
using System.Windows.Input;

namespace Tobi.Common.UI
{
    [TypeConverter(typeof(KeyGestureStringConverter))]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public class KeyGestureString : KeyGesture
    {
        public KeyGestureString(Key key)
            : base(key)
        {
        }

        public KeyGestureString(Key key, ModifierKeys modifiers)
            : base(key, modifiers)
        {
        }

        public KeyGestureString(Key key, ModifierKeys modifiers, string displayString)
            : base(key, modifiers, displayString)
        {
        }

        public override string ToString()
        {
            return KeyGestureStringConverter.Convert(this);
        }

        public override bool Equals(object obj)
        {
            var otherKG = obj as KeyGestureString;
            if (otherKG == null) return false;
            return otherKG.Key == Key && otherKG.Modifiers == Modifiers;
        }
    }
}
