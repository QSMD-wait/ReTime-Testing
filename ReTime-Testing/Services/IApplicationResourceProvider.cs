using System.Collections.Generic;
using System.Windows;

namespace ReTime_Testing.Services;

public interface IApplicationResourceProvider
{
    IList<ResourceDictionary> GetMergedDictionaries();
}