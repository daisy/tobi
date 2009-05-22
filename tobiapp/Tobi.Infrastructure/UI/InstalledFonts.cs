using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;

namespace Tobi.Infrastructure.UI
{
    /// <summary>
    /// 
    /// NOTE: a better option for WPF is to use System.Windows.Media.Fonts.SystemFontFamilies instead of System.Drawing.FontFamily
    /// 
    /*    1. <UserControl  
   2.     x:Class="FontManager.InstalledFontDisplay"  
   3.     xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"  
   4.     xmlns:drawing="clr-namespace:System.Drawing;assembly=System.Drawing"  
   5.     xmlns:m="clr-namespace:FontManager"  
   6.     xmlns:sys="clr-namespace:System.Collections.Generic;assembly=mscorlib"  
   7.     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">  
   8.     <UserControl.Resources>  
   9.         <Style x:Key="FontStyle">  
  10.             <Setter Property="Control.FontFamily" Value="{Binding Name}" />  
  11.             <Setter Property="Control.FontSize" Value="16" />  
  12.         </Style>  
  13.         <DataTemplate x:Key="FontTemplate">  
  14.             <StackPanel VirtualizingStackPanel.IsVirtualizing="True">  
  15.                 <TextBlock  
  16.                     Text="{Binding Name}"  
  17.                     ToolTip="{Binding Name}"  
  18.                     Style="{StaticResource FontStyle}" />  
  19.             </StackPanel>  
  20.         </DataTemplate>  
  21.         <ObjectDataProvider x:Key="FontProvider" ObjectType="{x:Type m:InstalledFonts}"/>  
  22.     </UserControl.Resources>  
  23.     <ComboBox  
  24.             VerticalAlignment="Top"  
  25.             ItemsSource="{Binding Source={StaticResource FontProvider}}"  
  26.             ItemTemplate="{StaticResource FontTemplate}" />  
  27.   
  28. </UserControl>  
    */
    /// </summary>
    public class InstalledFonts : List<FontFamily>
    {
        public InstalledFonts()
        {
            var fonts = new InstalledFontCollection();
            AddRange(fonts.Families);
        }
    }
}
