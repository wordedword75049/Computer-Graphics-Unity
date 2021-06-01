using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using UnityGoogleDrive;
using UnityEngine.Video;
using Vuforia;

public class InstantImageTarget : MonoBehaviour
{
    private UnityEngine.Video.VideoPlayer videoPlayer;
    private TrackableEventHandler handler;
    private UnityGoogleDrive.Data.Permission p;
    private string image_id;
    private string video_id;
    private string image_uuid_s;
    private string video_uuid_s;
    private bool first_flag;
    private bool second_flag;
    private string result;

    void Start()
    {

        bool image_flag = false;
        bool video_flag = false;

        var image = File.ReadAllBytes("./upload/image.jpg");
        Guid image_uuid = Guid.NewGuid();
        this.image_uuid_s = image_uuid.ToString();
        var file = new UnityGoogleDrive.Data.File() { Name = "CG_"+this.image_uuid_s+".jpg", Content = image};
        GoogleDriveFiles.CreateRequest request = GoogleDriveFiles.Create(file);
        request.Send().OnDone += res_image =>
        {
            Debug.Log("Image OK");
            image_flag = true;
            if (image_flag == true && video_flag == true) 
            {
                this.first_flag = true;
            }
        };


        var video = File.ReadAllBytes("./upload/video.mp4");
        Guid video_uuid = Guid.NewGuid();
        this.video_uuid_s = video_uuid.ToString();
        var vfile = new UnityGoogleDrive.Data.File() { Name = "CG_" + this.video_uuid_s + ".mp4", Content = video};
        GoogleDriveFiles.CreateRequest vrequest = GoogleDriveFiles.Create(vfile);
        vrequest.Send().OnDone += res_video =>
        {
            Debug.Log("Video OK");
            Debug.Log("Sleeping");
            System.Threading.Thread.Sleep(10000);
            Debug.Log("Sleeping End");
            video_flag = true;
            if (image_flag == true && video_flag == true)
            {
                this.first_flag = true;
            }
        };

        
    }


    void GoogleFinder()
    {
        GoogleDriveFiles.List().Send().OnDone += fileList =>
        {
            Debug.Log("Hello inside1");
            for (int i = 0; i < fileList.Files.Count; i++)
            {
                if (fileList.Files[i].Name == "CG_" + this.image_uuid_s + ".jpg")
                {
                    Debug.Log(fileList.Files[i].Name);
                    Debug.Log(fileList.Files[i].Id);
                    Debug.Log("Hello inside3");
                    GoogleDriveFiles.Download(fileList.Files[i].Id).Send().OnDone += image =>
                    {
                        this.image_id = image.Id;
                        Debug.Log("Hello inside4");
                        Debug.Log(this.image_id);
                        //Debug.Log(image.ContentHints.Thumbnail.Image);
                        Debug.Log(image.Id.GetType());
                        Debug.Log(image_id);
                    };
                }
                if (fileList.Files[i].Name == "CG_" + this.video_uuid_s + ".mp4")
                {
                    Debug.Log(fileList.Files[i].Name);
                    Debug.Log(fileList.Files[i].Id);
                    Debug.Log("Hello inside3");
                    GoogleDriveFiles.Download(fileList.Files[i].Id).Send().OnDone += video =>
                    {
                        this.video_id = video.Id;
                        Debug.Log("Hello inside4");
                        Debug.Log(this.video_id);
                        //Debug.Log(image.ContentHints.Thumbnail.Image);
                        Debug.Log(video.Id.GetType());
                        Debug.Log(video_id);
                    };
                }
            }
        };
        this.second_flag = false;
    }




    void Update()
    {
        if (this.first_flag == true)
        {
            
            GoogleFinder();
            this.first_flag = false;
        }
        if (this.second_flag != true && this.image_id != null)
        {
            //VuforiaARController.Instance.RegisterVuforiaStartedCallback(CreateImageTargetFromSideloadedTexture);
            StartCoroutine(CreateImageTargetFromDownloadedTexture());
            this.second_flag = true;
        }
    }
    IEnumerator CreateImageTargetFromDownloadedTexture()
    {

        UnityGoogleDrive.Data.FileList fileLists = new UnityGoogleDrive.Data.FileList();



        //UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("https://drive.google.com/uc?export=download&id=" + image_id);
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("https://drive.google.com/uc?id=" + image_id+ "&export=download"))
        {
            Debug.Log("https://drive.google.com/uc?export=download&id=" + this.image_id);
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
            }
            else
            {

                var objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();


                
                // Get downloaded texture once the web request completes
                var texture = DownloadHandlerTexture.GetContent(uwr);
                byte[] bytes = texture.EncodeToJPG();
                File.WriteAllBytes("./Assets/StreamingAssets/Vuforia/myTarget.jpg", bytes);

                // get the runtime image source and set the texture
                var runtimeImageSource = objectTracker.RuntimeImageSource;
                //runtimeImageSource.SetImage(texture, 0.7f, "100Rubles");
                runtimeImageSource.SetFile(VuforiaUnity.StorageType.STORAGE_APPRESOURCE, "Vuforia/myTarget.jpg", 0.7f, "100Rubles");

                // create a new dataset and use the source to create a new trackable
                var dataset = objectTracker.CreateDataSet();
                var trackableBehaviour = dataset.CreateTrackable(runtimeImageSource, "100Rubles");

                // add the DefaultTrackableEventHandler to the newly created game object
                trackableBehaviour.gameObject.AddComponent<TrackableEventHandler>();

                // activate the dataset
                objectTracker.ActivateDataSet(dataset);

                // TODO: add virtual content as child object(s)
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.transform.SetParent(trackableBehaviour.gameObject.transform);
                Quaternion rotation = Quaternion.Euler(0, 180, 0);
                plane.transform.rotation = rotation;
                plane.transform.localScale = new Vector3(0.07f, 1, 0.031f);
                videoPlayer = plane.AddComponent<UnityEngine.Video.VideoPlayer>();
                videoPlayer.playOnAwake = false;
                using (UnityWebRequest vuwr = UnityWebRequest.Get("https://drive.google.com/uc?id=" + video_id + "&export=download"))
                {
                    Debug.Log("https://drive.google.com/uc?export=download&id=" + this.video_id);
                    yield return vuwr.SendWebRequest();

                    if (vuwr.isNetworkError || vuwr.isHttpError)
                    {
                        Debug.Log(vuwr.error);
                    }
                    else
                    {
                        File.WriteAllBytes("./load/video.mp4", vuwr.downloadHandler.data);
                        
                    }
                }

                videoPlayer.url = "./load/video.mp4";


                handler = trackableBehaviour.gameObject.GetComponent<TrackableEventHandler>();
                handler.OnTargetFound = new UnityEngine.Events.UnityEvent();
                handler.OnTargetFound.AddListener(videoPlayer.Play);
                handler.OnTargetLost = new UnityEngine.Events.UnityEvent();
                handler.OnTargetLost.AddListener(videoPlayer.Pause);
            }
        }
    }

    void BuildResultString(UnityGoogleDrive.Data.File file)
    {
        result = string.Format("Name: {0} Size: {1:0.00}MB Created: {2:dd.MM.yyyy HH:MM:ss}",
                file.Name,
                file.Size * .000001f,
                file.CreatedTime);
        Debug.Log(result);
    }
}