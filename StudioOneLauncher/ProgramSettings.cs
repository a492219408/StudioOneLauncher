namespace StudioOneLauncher;

/// <summary>
/// 表示启动器程序的设置类，包含程序路径、项目路径以及自动保存相关设置。
/// </summary>
public class ProgramSettings
{
    /// <summary>
    /// 获取或设置要启动的程序的路径。
    /// 例如，Studio One 的可执行文件路径。
    /// </summary>
    public string? ProgramPath { get; set; }

    /// <summary>
    /// 获取或设置项目文件夹的路径。
    /// 例如，Studio One 项目文件所在的文件夹路径。
    /// </summary>
    public string? ProjectFolderPath { get; set; }

    /// <summary>
    /// 获取或设置是否启用了自动保存功能。
    /// 该属性指示程序是否需要处理自动保存的文件。
    /// </summary>
    public bool UsedAutoSave { get; set; }

    /// <summary>
    /// 获取或设置当前使用的 .song 文件路径。
    /// 在遍历项目文件夹时找到的第一个 .song 文件路径会存储在此属性中。
    /// </summary>
    public string? SongFilePath { get; set; }

    /// <summary>
    /// 获取自动保存的文件路径。
    /// 自动保存文件通常会附加 ".autosave" 扩展名。
    /// 例如，如果 <see cref="SongFilePath"/> 为 "project.song"，
    /// 则 <see cref="SongAutoSaveFilePath"/> 会返回 "project.song.autosave"。
    /// </summary>
    public string SongAutoSaveFilePath => SongFilePath + ".autosave";

    /// <summary>
    /// 获取程序的文件名（不含路径）。
    /// 例如，如果 <see cref="ProgramPath"/> 为 "C:/Program Files/StudioOne/StudioOne.exe"，
    /// 则此属性会返回 "StudioOne.exe"。
    /// </summary>
    public string? ProgramFileName => Path.GetFileName(ProgramPath);
}