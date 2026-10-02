using System;

namespace Mda8086Kit.Core.Abstractions
{
    public interface IWarningSink
    {
        void Warn(string code, string message);
    }
}
