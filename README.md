# Easily read/write files on Phones/Tablets that connect to your PC via USB. 

## Deal easily with any external drive, even if you can't find it in the available drives (DriveInfo.GetDrives)

As soon as you plug in a Portable Drive (Phone/Tablet), it will show up in the available drives, here:

    drive_root.inst.drives

Enumerate all the pictures you've taken (a0 - first android drive)

    Console.WriteLine(
      string.Join(",",drive_root.inst.parse_folder("[a0]:/*/dcim/camera").files.Select(f => f.name)));
  
Copy to HDD, the last 100 photos you took

    // a0 - the first connected Android device
    static void copy_to_hdd() {
        var files = drive_root.inst.parse_folder("[a0]:/*/dcim/camera")
            .files.OrderBy(f => f.last_write_time).ToList();
        if (files.Count > 100) 
            files = files.GetRange(files.Count - 100, 100);
        foreach (var f in files) 
            f.copy_sync("c:/my_camera_files");
    }

Show all Albums you created on your Android Phone

    var dcim = drive_root.inst.parse_folder("[a0]:/*/dcim");
    foreach (var f in dcim.child_folders)
        Console.WriteLine(f.full_path);
  
Copy the last taken photo, to its parent folder

    var camera = drive_root.inst.parse_folder("[a0]:/phone/dcim/camera");
    var last_file = camera.files.OrderBy(f => -f.last_write_time.Ticks).First();
    last_file.copy_sync(camera.parent.full_path);

If you want more info, I've written a rather large article on [codeproject](https://www.codeproject.com/Articles/1213684/External-Drives-Library-Part-Dealing-with-USB-Conn)

Projects using external_drive_lib:
* [Phot-Awe](http://www.phot-awe.com)

Got a project using external_drive_lib? Let me know!
