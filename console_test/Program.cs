using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using external_drive_lib;
using external_drive_lib.bulk;
using external_drive_lib.interfaces;
using external_drive_lib.monitor;
using external_drive_lib.util;

namespace console_test
{
    class Program
    {
        static string new_temp_path() {
            var temp_dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\external_drive_temp\\test-" + DateTime.Now.Ticks;
            Directory.CreateDirectory(temp_dir);
            return temp_dir;
        }

        static void example_show_all_portable_drives() {
            Console.WriteLine("Enumerating Portable Drives:");
            var portable_drives = drive_root.inst.drives.Where(d => d.type.is_portable()).ToList();
            foreach ( var pd in portable_drives)
                Console.WriteLine("Drive Unique ID: " + pd.unique_id + ", friendly name=" + pd.friendly_name 
                    + ", type=" + pd.type + ", available=" + pd.is_available());
            if ( portable_drives.Count < 1)
                Console.WriteLine("No Portable Drives connected");
        }

        static string files_count_suffix(IEnumerable<IFile> files) {
            var count = files.Count();
            var suffix = count > 0 ? " - " + count + " files" : "";
            return suffix;
        }

        static void traverse_folder(IFolder folder, bool dump_file_count_only, int level) {
            var suffix = dump_file_count_only ? files_count_suffix(folder.files): "";
            Console.WriteLine(new string(' ', level * 2) + folder.name + suffix);
            if ( !dump_file_count_only)
                dump_files(folder.files, level);

            foreach ( var child in folder.child_folders)
                traverse_folder(child, dump_file_count_only, level + 1);
        }

        static void dump_files(IEnumerable<IFile> files, int indent) {
            foreach ( var f in files)
                Console.WriteLine(new string(' ', indent * 2) + f.name + ", size=" + f.size + ", modified=" + f.last_write_time);
        } 

        static void traverse_drive(IDrive d, bool dump_file_count_only) {
            Debug.Assert(d.type.is_portable());

            var suffix = dump_file_count_only ? files_count_suffix(d.files) : "";
            Console.WriteLine("Drive " + d.unique_id + suffix);
            if ( !dump_file_count_only)
                dump_files(d.files, 0);
            foreach ( var folder in d.folders)
                traverse_folder(folder, dump_file_count_only, 1);
        }

        static void example_traverse_first_portable_drive(bool dump_file_count_only) {
            Console.WriteLine("Traversing First Portable Drive");
            var portable_drives = drive_root.inst.drives.Where(d => d.type.is_portable()).ToList();
            if (portable_drives.Count > 0) 
                traverse_drive(portable_drives[0], dump_file_count_only);
            else
                Console.WriteLine("No Portable Drives connected");
        }

        static void example_enumerate_all_android_albums() {
            Console.WriteLine("Enumerating all albums on First Android Drive");
            if (drive_root.inst.drives.Any(d => d.type.is_android())) {
                foreach ( var folder in drive_root.inst.parse_folder("[a0]:/*/dcim").child_folders)
                    Console.WriteLine(folder.name + " - " + folder.files.Count() + " files");
            }
            else 
                Console.WriteLine("No Android Drive Connected");
        }

        static void example_copy_all_camera_photos_to_hdd() {
            Console.WriteLine("Copying all photos you took on your first Android device");
            var camera = drive_root.inst.try_parse_folder("[a0]:/*/dcim/camera");
            if (camera != null) {
                DateTime start = DateTime.Now;
                var temp = new_temp_path();
                Console.WriteLine("Copying to " + temp);
                bulk.bulk_copy_sync(camera.files, temp);
                Console.WriteLine("Copying to " + temp + " - complete, took " + (int)(DateTime.Now - start).TotalMilliseconds + " ms" );
            }
            else 
                Console.WriteLine("No Android Drive Connected");
        }

        /* Note: this shows progress (that is, after each copied file). However, it will be slower than the bulk copy
         * (bulk copying can do some optimizations, but for now, you don't kwnow the progress)
         */
        static void example_copy_all_camera_photos_to_hdd_with_progress() {
            Console.WriteLine("Copying all photos you took on your first Android device");
            var camera = drive_root.inst.try_parse_folder("[a0]:/*/dcim/camera");
            if (camera != null) {
                var temp = new_temp_path();
                DateTime start = DateTime.Now;
                var files = camera.files.ToList();
                var idx = 0;
                foreach (var file in files) {
                    Console.Write(file.full_path + " to " + temp + "(" + ++idx + " of " + files.Count + ")");
                    file.copy_sync(temp);
                    Console.WriteLine(" ...done");
                }
                Console.WriteLine("Copying to " + temp + " - complete, took " + (int)(DateTime.Now - start).TotalMilliseconds + " ms" );
            }
            else 
                Console.WriteLine("No Android Drive Connected");
        }

        static void example_copy_latest_photo_to_hdd() {
            Console.WriteLine("Copying latest photo from your Android device to HDD");    
            var camera = drive_root.inst.try_parse_folder("[a0]:/*/dcim/camera");
            if (camera != null) {
                var temp = new_temp_path();
                var files = camera.files.OrderBy(f => f.last_write_time).ToList();
                if (files.Count > 0) {
                    var latest_file = files.Last();
                    Console.WriteLine("Copying " + latest_file.full_path + " to " + temp);
                    latest_file.copy_sync(temp);
                    Console.WriteLine("Copying " + latest_file.full_path + " to " + temp + " - complete");
                }
                else 
                    Console.WriteLine("You have no Photos");
            }
            else 
                Console.WriteLine("No Android Drive Connected");
        }

        static void example_find_biggest_photo_in_size() {
            Console.WriteLine("Copying latest photo from your Android device to HDD");    
            var camera = drive_root.inst.try_parse_folder("[a0]:/*/dcim/camera");
            if (camera != null) {
                var files = camera.files.ToList();
                if (files.Count > 0) {
                    var max_size = files.Max(f => f.size);
                    var biggest_file = files.First(f => f.size == max_size);
                    Console.WriteLine("Your biggest photo is " + biggest_file.full_path + ", size=" + biggest_file.size);
                }
                else 
                    Console.WriteLine("You have no Photos");
            }
            else 
                Console.WriteLine("No Android Drive Connected");            
        }

        static long file_size(IFile f) {
            return f?.size ?? 0;
        }
        static IFile get_biggest_file(List<IFile> files) {
            if (files.Count < 1)
                return null;
            var max_size = files.Max(f => f.size);
            var biggest_file = files.First(f => f.size == max_size);
            return biggest_file;
        }

        static IFile get_biggest_file(IFolder folder) {
            var files = folder.files.ToList();
            if (files.Count < 1)
                return null;
            var max_size = files.Max(f => f.size);
            var biggest_file = files.First(f => f.size == max_size);
            return biggest_file;
        }

        static IFile get_biggest_file_recursive(IFolder folder) {
            var biggest = get_biggest_file(folder);
            foreach (var child in folder.child_folders) {
                var child_biggest = get_biggest_file_recursive(child);
                if (file_size( biggest) < file_size( child_biggest))
                    biggest = child_biggest;
            }
            return biggest;
        }

        static IFile get_biggest_file(IDrive d) {
            IFile biggest = get_biggest_file(d.files.ToList());
            foreach (var child in d.folders) {
                var child_biggest = get_biggest_file_recursive(child);
                if (file_size( biggest) < file_size( child_biggest))
                    biggest = child_biggest;
            }
            return biggest;
        }


        static void example_find_biggest_file_on_first_portable_device() {
            Console.WriteLine("Findind biggest file on First Portable Device");
            var portable_drives = drive_root.inst.drives.Where(d => d.type.is_portable()).ToList();
            if (portable_drives.Count > 0 ) {
                if (portable_drives[0].is_available()) {
                    var biggest = get_biggest_file(portable_drives[0]);
                    if (biggest != null)
                        Console.WriteLine("Biggest file on device " + biggest.full_path + ", " + biggest.size);
                    else
                        Console.WriteLine("You have no files on device");
                }
                else Console.WriteLine("First Portable device is not available");
            }
            else
                Console.WriteLine("No Portable Drives connected");
        }

        static void example_wait_for_first_connected_device() {
            Console.WriteLine("Waiting for you to plug the first portable device");
            while (true) {
                var portable_drives = drive_root.inst.drives.Where(d => d.type.is_portable());
                if (portable_drives.Any())
                    break;
            }
            Console.WriteLine("Waiting for you to make the device availble");
            while (true) {
                var d = drive_root.inst.try_get_drive("[p0]:/");
                if (d != null && d.is_available())
                    break;
            }
            example_show_all_portable_drives();
        }

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure( new FileInfo("console_test.exe.config"));

            example_wait_for_first_connected_device();

            example_show_all_portable_drives();

            bool dump_file_count_only = true;
            example_traverse_first_portable_drive(dump_file_count_only);

            example_enumerate_all_android_albums();
            example_copy_all_camera_photos_to_hdd();
            example_copy_all_camera_photos_to_hdd_with_progress();

            example_copy_latest_photo_to_hdd();
            example_find_biggest_photo_in_size();
            example_find_biggest_file_on_first_portable_device();

            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
