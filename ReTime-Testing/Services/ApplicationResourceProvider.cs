using System.Collections.Generic;
using System.Windows;

namespace ReTime_Testing.Services;

public class ApplicationResourceProvider : IApplicationResourceProvider
{
    public IList<ResourceDictionary> GetMergedDictionaries()
    {
        var app = Application.Current;
        if (app == null)
            throw new InvalidOperationException("Application.Current 不可用");

        return app.Resources.MergedDictionaries;
    }
}