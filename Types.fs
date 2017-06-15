namespace EpisodeWebMvc

type DirectoryOptions() =
    member val KeyFileDirectory = ""
        with get, set
    member val DecodeTargetDirectory = ""
        with get, set
    member val ProcessedTargetDirectory = ""
        with get, set