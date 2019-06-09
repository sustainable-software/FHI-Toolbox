using System;

namespace FhiModel.Common
{
    public interface ILocated
    {
        Location Location { get; }
        String Name { get; }
    }
}