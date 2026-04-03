using Ps2IsoTools.UDF;
using System.Text;

record FragmentPatch(int address, byte[] bytes);
struct ELFSection {
    public uint name_offset;

    public uint offset;
    public uint size;

    public string name;
};
class FragmentPatcher {
    static void Main(string[] args) {
        string inputPath, outputPath, basePath = AppContext.BaseDirectory;
        if (args.Length == 0) {
            inputPath  = Path.Combine(basePath, "dotHack fragment (EN).iso");
            outputPath = Path.Combine(basePath, "dotHack fragment (EN) ShortcutPatch.iso");
        } else if (args.Length == 1) {
            inputPath  = args[0];

            string? inputDirectory = Path.GetDirectoryName(args[0]);
            if (inputDirectory == null) {
                Console.WriteLine("Null path?");
                return;
            }
            outputPath = Path.Combine(inputDirectory, $"{Path.GetFileNameWithoutExtension(args[0])} ShortcutPatch.iso"); ;
        } else {
            inputPath  = args[0];
            outputPath = args[1];
        }

        if (!File.Exists(inputPath)) {
            Console.WriteLine($"Couldn't find {inputPath}");
            return;
        }

        if (File.Exists(outputPath)) {
            File.Delete(outputPath);
        }
        try {
            File.Copy(inputPath, outputPath);
        } catch (Exception e) {
            Console.WriteLine(e.Message);
            return;
        }

        if (!File.Exists(outputPath)) {
            Console.WriteLine($"Couldn't create {outputPath}");
            return;
        }


        ApplyShortcutPatch(outputPath);
    }

    static byte[] getPatchBytes(string path) {
        byte[] patch_data;
        Dictionary<string, ELFSection> sections;
        using (var fs = File.OpenRead(path)) {
            using (var patch_file = new BinaryReader(fs)) {
                patch_file.BaseStream.Seek(0x20, SeekOrigin.Begin);
                uint e_shoff = patch_file.ReadUInt32();

                patch_file.BaseStream.Seek(0x2E, SeekOrigin.Begin);
                int e_shentsize = patch_file.ReadInt16();
                int e_shnum = patch_file.ReadInt16();
                int e_shstrndx = patch_file.ReadInt16();

                List<ELFSection> section_list = [];
                for (int i = 0; i < e_shnum; i++) {
                    ELFSection section = new();
                    long section_start = e_shoff + i * e_shentsize;


                    patch_file.BaseStream.Seek(section_start, SeekOrigin.Begin);
                    section.name_offset = patch_file.ReadUInt32();
                    patch_file.BaseStream.Seek(section_start + 16, SeekOrigin.Begin);
                    section.offset = patch_file.ReadUInt32();
                    section.size = patch_file.ReadUInt32();

                    section_list.Add(section);
                }
                patch_file.BaseStream.Seek(section_list[e_shstrndx].offset, SeekOrigin.Begin);
                byte[] name_bytes = new byte[section_list[e_shstrndx].size];
                patch_file.Read(name_bytes, 0, (int)section_list[e_shstrndx].size);

                sections = section_list.Select(section => {
                    int start = (int)section.name_offset;
                    int end = start;
                    while (name_bytes[end] != 0) end++;
                    section.name = Encoding.UTF8.GetString(name_bytes, start, end - start);
                    return section;
                }).ToDictionary(section => section.name);

                //foreach ((string name, ELFSection section) in sections) {
                //    Console.WriteLine($"{section.name}: start:{section.offset}, size:{section.size}");
                //}

                ELFSection text = sections[".text"];
                ELFSection rodata = sections[".rodata"];


                uint start = text.offset;
                uint end = rodata.offset + rodata.size;

                patch_data = new byte[end - start];
                patch_file.BaseStream.Seek(start, SeekOrigin.Begin);
                patch_file.Read(patch_data);
            }
        }
        Console.WriteLine($"{path} patch size: {patch_data.Length}");
        return patch_data;
    }

    static bool ApplyShortcutPatch(string isoPath) {
        string offlinePatchPath = @"ShortcutPatch\offline.erl";
        string onlinePatchPath  = @"ShortcutPatch\online.erl";

        if (!File.Exists(offlinePatchPath)) {
            Console.WriteLine($"Couldn't find {offlinePatchPath}");
            return false;
        }
        if (!File.Exists(onlinePatchPath)) {
            Console.WriteLine($"Couldn't find {onlinePatchPath}");
            return false;
        }


        FragmentPatch offlineHook = new(0x005a7244 - 0x537600,
                                        [0x2d, 0x20, 0x80, 0x02,
                                         0x38, 0xB1, 0x1B, 0x0C]
                                       );

        FragmentPatch offlinePatch = new(0x006ec4e0 + 0x100 - 0x537600,
                                         getPatchBytes(offlinePatchPath));

        FragmentPatch onlineHook = new(0x00230894 - 0xFFF00,
                                       [0x2d, 0x20, 0x80, 0x02,
                                        0x32, 0x21, 0x18, 0x0C,]
                                      );

        FragmentPatch onlinePatch = new(0x006084c8 + 0x100 - 0xFFF00,
                                        getPatchBytes(onlinePatchPath));

        Dictionary<string, FragmentPatch[]> fileToPatches = new(){
            { "DATA\\gcmnf.prg", [offlineHook, offlinePatch]},
            { "HACK_00.ELF", [onlineHook, onlinePatch]},
        };
        using (var editor = new UdfEditor(isoPath)) {
            foreach ((string file, FragmentPatch[] patches) in fileToPatches) {
                var fileId = editor.GetFileByName(file);
                if (fileId is not null) {
                    using (BinaryWriter bw = new(editor.GetFileStream(fileId))) {
        
                        foreach (FragmentPatch patch in patches) {
                            bw.Seek(patch.address, SeekOrigin.Begin);
                            bw.Write(patch.bytes);
                            Console.WriteLine($"Applied patch to {file} at {patch.address}");
                        }
                    }
                } else {
                    Console.WriteLine($"Failed to find {file} in target iso");
                }
            }
        
        }
        return true;
    }
}





