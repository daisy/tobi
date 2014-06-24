using System;
using System.Linq;
using System.Windows.Data;

namespace Tobi.Infrastructure.UI.FileDialog
{
    public class FileSysemInformationConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                DirInfo nodeToExpand = value as DirInfo;
                if (nodeToExpand == null)
                    return null;

                //return the subdirectories of the Current Node
                if ((ObjectType)nodeToExpand.DirType == ObjectType.MyComputer)
                {
                    return (from sd in FileSystemExplorerService.GetRootDirectories()
                            select new DirInfo(sd)).ToList();
                }
                else
                {
                    return (from dirs in FileSystemExplorerService.GetChildDirectories(nodeToExpand.Path)
                            select new DirInfo(dirs)).ToList();
                }

            }
            catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
