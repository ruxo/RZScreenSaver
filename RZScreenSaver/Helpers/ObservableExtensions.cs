using System;
using System.Reactive.Disposables;

namespace RZScreenSaver.Helpers;

public static class ObservableExtensions
{
    public static IDisposable DisposableWith(this IDisposable d, CompositeDisposable disposables) {
        disposables.Add(d);
        return d;
    }
}