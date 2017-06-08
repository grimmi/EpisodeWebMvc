namespace OtrDecoder

type DecodeOptions() = 

    member val DecoderPath = "" with get, set
    member val Email = "" with get, set
    member val Password = "" with get, set
    member val OutputDirectory = "" with get, set
    member val InputDirectory = "" with get, set
    member val FileExtension = ".otrkey" with get, set
    member val ForceOverwrite = true with get, set
    member val CreateDirectories = true with get, set
    member val AutoCut = true with get, set
    member val ContinueWithoutCutlist = true with get, set