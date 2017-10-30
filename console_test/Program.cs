using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using external_drive_lib;
using external_drive_lib.interfaces;

namespace console_test
{
    class Program
    {
        static void dump_folders_and_files(IEnumerable<IFolder> folders, IEnumerable<IFile> files, int indent) {
            Console.WriteLine("");
            Console.WriteLine("Level " + (indent+1));
            Console.WriteLine("");
            foreach ( var f in folders)
                Console.WriteLine(new string(' ', indent * 2) + f.full_path);
            foreach ( var f in files)
                Console.WriteLine(new string(' ', indent * 2) + f.full_path + " " + f.size + " " + f.last_write_time);
        } 

        static void traverse_drive(IDrive d, int levels) {
            var folders = d.folders.ToList();
            // level 1
            dump_folders_and_files(folders, d.files, 0);
            for (int i = 1; i < levels; ++i) {
                var child_folders = new List<IFolder>();
                var child_files = new List<IFile>();
                foreach (var f in folders) {
                    try {
                        child_folders.AddRange(f.child_folders);
                    } catch {
                        // could be unauthorized access
                        Console.WriteLine(new string(' ', i * 2) + f.full_path + " *** UNAUTHORIZED folders");
                    }
                    try {
                        child_files.AddRange(f.files);
                    } catch {
                        // could be unauthorized access
                        Console.WriteLine(new string(' ', i * 2) + f.full_path + " *** UNAUTHORIZED files");
                    }
                }
                dump_folders_and_files(child_folders, child_files, i);
                folders = child_folders;
            }
        }

        static void Main(string[] args)
        {
            traverse_drive( drive_root.inst.get_drive("d:\\"), 3);
        }
    }
}
