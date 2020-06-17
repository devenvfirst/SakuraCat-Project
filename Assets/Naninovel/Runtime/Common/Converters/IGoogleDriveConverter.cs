// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Implementation is able to convert exported google drive files to <typeparamref name="TResult"/>.
    /// </summary>
    public interface IGoogleDriveConverter<TResult> : IRawConverter<TResult>
    {
        string ExportMimeType { get; }
    }
}
