using System;

namespace gis_photo_sharing_app.Application.Common.Exceptions;

public class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message)
    {
    }
}
