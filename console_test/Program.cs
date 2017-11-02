using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
                Console.WriteLine(new string(' ', indent * 2) + f.full_path + " " + f.name);
            foreach ( var f in files)
                Console.WriteLine(new string(' ', indent * 2) + f.full_path + " " + f.name + " " + f.size + " " + f.last_write_time);
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
                    } catch(Exception e) {
                        // could be unauthorized access
                        Console.WriteLine(new string(' ', i * 2) + f.full_path + " *** UNAUTHORIZED folders " + e);
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

        // these are files from my drive
        static void test_win_parse_files() {
            Debug.Assert(drive_root.inst.parse_file("D:\\cool_pics\\a00\\b0\\c0\\20161115_035718.jPg").size == 4532595);
            Debug.Assert(drive_root.inst.parse_file("D:\\cool_pics\\a00\\b0\\c0\\20161115_104952.jPg").size == 7389360);
            Debug.Assert(drive_root.inst.parse_folder("D:\\cool_pics\\a10").files.Count() == 25);
            Debug.Assert(drive_root.inst.parse_folder("D:\\cool_pics").child_folders.Count() == 8);

            Debug.Assert(drive_root.inst.parse_file("D:\\cool_pics\\a00\\b0\\c0\\20161115_035718.jPg").full_path == "D:\\cool_pics\\a00\\b0\\c0\\20161115_035718.jPg");
            Debug.Assert(drive_root.inst.parse_folder("D:\\cool_pics\\a10").full_path == "D:\\cool_pics\\a10");
        }

        static void test_parent_folder() {
            Debug.Assert(drive_root.inst.parse_file("D:\\cool_pics\\a00\\b0\\c0\\20161115_035718.jPg").folder.full_path == "D:\\cool_pics\\a00\\b0\\c0");
            Debug.Assert(drive_root.inst.parse_file("D:\\cool_pics\\a00\\b0\\c0\\20161115_035718.jPg").folder.parent.full_path == "D:\\cool_pics\\a00\\b0");
        }


        ///////////////////////////////////////////////////////////////////
        // Android tests

        const string android_prefix = "{galaxy s6}";

        static void android_test_parse_files() {
            Debug.Assert(drive_root.inst.parse_file(android_prefix + ":\\phone/dcim/camera/20171005_121557.jPg").size == 4598747);
            Debug.Assert(drive_root.inst.parse_file(android_prefix + ":\\phone/dcim/camera/20171005_121601.jPg").size == 3578988);
            Debug.Assert(drive_root.inst.parse_folder(android_prefix + ":\\phone/dcim/camera") != null);

            Debug.Assert(drive_root.inst.parse_folder(android_prefix + ":\\phone/dcim/camera").full_path.ToLower() == android_prefix + ":\\phone\\dcim\\camera");

        }

        static void android_test_parent_folder() {
            // ... uses file.parent
            Debug.Assert(drive_root.inst.parse_file(android_prefix + ":\\phone/dcim/camera/20171005_121557.jPg").folder.full_path.ToLower() 
                         == android_prefix + ":\\phone\\dcim\\camera");
            // ... uses file.parent and folder.parent
            Debug.Assert(drive_root.inst.parse_file(android_prefix + ":\\phone/dcim/camera/20171005_121557.jPg").folder.parent.full_path.ToLower() 
                         == android_prefix + ":\\phone\\dcim");

            Debug.Assert(drive_root.inst.parse_file(android_prefix + ":\\phone/dcim/camera/20171005_121557.jPg").full_path.ToLower() 
                         == android_prefix + ":\\phone\\dcim\\camera\\20171005_121557.jpg");
            Debug.Assert(drive_root.inst.parse_file(android_prefix + ":\\phone/dcim/camera/20171005_121601.jPg").full_path.ToLower() 
                         == android_prefix + ":\\phone\\dcim\\camera\\20171005_121601.jpg");            

        }

        static void android_test_create_delete_folder() {
            Debug.Assert(drive_root.inst.new_folder(android_prefix + ":/phone/dcim/testing123") != null);
            drive_root.inst.parse_folder(android_prefix + ":/phone/dcim/testing123").delete_sync();
            try {
                drive_root.inst.parse_folder(android_prefix + ":/phone/dcim/testing123");
                Debug.Assert(false);

            } catch {
                // ok - the folder should not exist anymore
            }            
        }

        static void android_test_copy_and_delete_file() {
            var camera = drive_root.inst.parse_folder(android_prefix + ":/phone/dcim/camera");
            var first_file = camera.files.ToList()[0];
            first_file.copy_sync(camera.parent.full_path);

            // copy : android to windows
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\external_drive_temp\\test-" + DateTime.Now.Ticks;
            Directory.CreateDirectory(dir);
            first_file.copy_sync(dir);
            var name = first_file.name;
            Debug.Assert(first_file.size == new FileInfo(dir + "\\" + name).Length);

            // copy: windows to android
            var renamed = dir + "\\" + name + ".renamed.jpg";
            File.Move(dir + "\\" + name, renamed);
            drive_root.inst.parse_file(renamed).copy_sync(android_prefix + ":/phone/dcim/");
            // FIXME clearly, this is not ideal, but apparently, the copy is somehow synchronnously, and it takes a short while
            //       for the folder to realize about the new copied file - not sure what to do at this time
            Thread.Sleep(2500);
            Debug.Assert(first_file.size == drive_root.inst.parse_file(android_prefix + ":/phone/dcim/" + name + ".renamed.jpg").size);
        }


        // what I want is to find out how fast is this, compared to Windows Explorer (roughly)
        // 80738 millis on 452 items (1.8Gb) in Debug
        // 77477 millis on 452 items (1.8Gb) in Release
        //
        // 67 secs copy from xplorer (clearly, this was a bulk copy)
        static void android_test_copy_full_dir_to_windows() {
            DateTime start = DateTime.Now;
            var dest_dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\external_drive_temp\\test-" + DateTime.Now.Ticks;
            Directory.CreateDirectory(dest_dir);
            var camera = drive_root.inst.parse_folder(android_prefix + ":/phone/dcim/camera");
            foreach (var f in camera.files) {
                Console.WriteLine(f.name);
                f.copy_sync(dest_dir);
            }
            var spent_time = (DateTime.Now - start).TotalMilliseconds;
            Console.WriteLine("spent " + spent_time.ToString("f2") + " ms");
        }

        // END OF Android tests
        ///////////////////////////////////////////////////////////////////

        // copies all files from this folder into a sub-folder we create
        // after we do that, we delete the sub-folder
        static void test_copy_and_delete_files(string src_path) {
            var src = drive_root.inst.parse_folder(src_path);
            var old_folder_count = src.child_folders.Count();
            var child_dir = src_path + "/child1/child2/child3/";
            var dest = src.drive.create_folder(child_dir);
            foreach ( var child in src.files)
                child.copy_sync(child_dir);
            long src_size = src.files.Sum(f => f.size);
            long dest_size = dest.files.Sum(f => f.size);
            Debug.Assert(src_size == dest_size);
            Debug.Assert(src.child_folders.Count() == old_folder_count + 1);
            foreach (var child in dest.files)
                child.delete_sync();

            var first_child = dest.parent.parent;
            first_child.delete_sync();

            Debug.Assert(src.child_folders.Count() == old_folder_count );
        }


        static void test_copy_files(string src_path, string dest_path) {
            var src = drive_root.inst.parse_folder(src_path);
            var dest = drive_root.inst.new_folder(dest_path);
            foreach ( var child in src.files)
                child.copy_sync(dest_path);

            // ugly, but since this happens somewhat sync, we need to sleep a bit before reading everything correctly (on android)
            Thread.Sleep(2500);
            long src_size = src.files.Sum(f => f.size);
            long dest_size = dest.files.Sum(f => f.size);
            Debug.Assert(src_size == dest_size);
        }

        static void test_copy_files_android_to_win_and_viceversa() {
            // first from android to win, then vice versa
            var temp_dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\external_drive_temp\\test-" + DateTime.Now.Ticks;
            Directory.CreateDirectory(temp_dir);
            test_copy_files(android_prefix + ":/phone/dcim/facebook", temp_dir);
            test_copy_files(temp_dir, android_prefix + ":/phone/dcim/facebook_copy");
            drive_root.inst.parse_folder(temp_dir).delete_sync();
            drive_root.inst.parse_folder(android_prefix + ":/phone/dcim/facebook_copy").delete_sync();            
        }

        static void Main(string[] args)
        {
            //traverse_drive( drive_root.inst.get_drive("d:\\"), 3);
            //test_win_parse_files();
            //test_parent_folder();
            //test_copy_and_delete_files("D:\\cool_pics\\a00\\b0\\c0\\");
            
            //traverse_drive( drive_root.inst.get_drive(android_prefix), 4);
            //android_test_parse_files();
            //android_test_parent_folder();
            //android_test_create_delete_folder();
            //android_test_copy_and_delete_file();
            //android_test_copy_full_dir_to_windows();
            // first from android to win, then vice versa
            android_test_copy_full_dir_to_windows();
        }
    }
}
