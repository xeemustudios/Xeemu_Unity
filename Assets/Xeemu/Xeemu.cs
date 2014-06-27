using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Facebook.MiniJSON;
using System;

public class Xeemu : MonoBehaviour {
	private string Manu_Username;
	public Texture CHALLANGED;
	public Texture Brag;
	public Texture ButtonTexture;
	public Texture facebook;

	public GUISkin MenuSkin; 
	public GUIStyle f_style;
	public Rect LoginButtonRect;                // Position of login button
	public float ChallengeDisplayTime;          // Number of seconds the request sent message is displayed for
	public Vector2 ButtonLogoOffset;            // Offset determining positioning of logo on buttons
	public float TournamentStep; 
	public float MouseScrollStep = 40;
	public int CoinBalance;
	public int NumLives;
	public int NumBombs;
	private static Xeemu instance;
	
	private static List<object>                 friends         = null;
	private static Dictionary<string, string>   profile         = null;
	private static List<object>                 scores          = null;
	private static Dictionary<string, Texture>  friendImages    = new Dictionary<string, Texture>();
	private Vector2 scrollPosition = Vector2.zero;
    private bool    haveUserPicture       = false;
	private float   tournamentLength      = 0;
	private int     tournamentWidth       = 512;
     private int     mainMenuLevel         = 0; // Level index of main menu
    private string popupMessage;
	private float popupTime;
	private float popupDuration;

	// Use this for initialization
	void Awake(){

		Util.Log("Awake");
		// allow only one instance of the Main Menu
		if (instance != null && instance != this)
		{
			Destroy(gameObject);
			return;
		}
		DontDestroyOnLoad(gameObject);
		instance = this;
		// Initialize FB SDK
		enabled = false;
		FB.Init(SetInit, OnHideUnity);
		}

	private void SetInit()
	{
		Util.Log("SetInit");
		enabled = true; // "enabled" is a property inherited from MonoBehaviour
		if (FB.IsLoggedIn) 
		{
			Util.Log("Already logged in");
			OnLoggedIn();
		}
	}

	private void OnHideUnity(bool isGameShown)
	{
		Util.Log("OnHideUnity");
		if (!isGameShown)
		{
			// pause the game - we will need to hide
			Time.timeScale = 0;
		}
		else
		{
			// start the game back up - we're getting focus again
			Time.timeScale = 1;
		}
	}
	void LoginCallback(FBResult result)
	{
		Util.Log("LoginCallback");
		if (FB.IsLoggedIn)
		{
			OnLoggedIn();
		}
	}
	void OnLoggedIn()
	{
		Util.Log("Logged in. ID: " + FB.UserId);
		
		// Reqest player info and profile picture
		FB.API("/me?fields=id,first_name,friends.limit(100).fields(first_name,id)", Facebook.HttpMethod.GET, APICallback);
		LoadPicture(Util.GetPictureURL("me", 128, 128),MyPictureCallback);
		
	}
	void APICallback(FBResult result)
	{
		Util.Log("APICallback");
		if (result.Error != null)
		{ 
			Util.LogError(result.Error);
			// Let's just try again
			FB.API("/me?fields=id,first_name,friends.limit(100).fields(first_name,id)", Facebook.HttpMethod.GET, APICallback);
			return;
		}
		profile = Util.DeserializeJSONProfile(result.Text);
		Manu_Username = profile["first_name"];
		friends = Util.DeserializeJSONFriends(result.Text);
	}
	void MyPictureCallback(Texture texture)
	{
		Util.Log("MyPictureCallback");
	   if (texture ==  null)
		{
			// Let's just try again
			LoadPicture(Util.GetPictureURL("me", 128, 128),MyPictureCallback);
			return;
		}
		ButtonTexture = texture;
	}
	void OnApplicationFocus( bool hasFocus ) 
	{
		Util.Log ("hasFocus " + (hasFocus ? "Y" : "N"));
	}
	
	// Convenience function to check if mouse/touch is the tournament area
	private bool IsInTournamentArea (Vector2 p)
	{
		return p.x > Screen.width-tournamentWidth;
	}
	
	
	// Scroll the tournament view by some delta
	private void ScrollTournament(float delta)
	{
		scrollPosition.y += delta;
		if (scrollPosition.y > tournamentLength - Screen.height)
			scrollPosition.y = tournamentLength - Screen.height;
		if (scrollPosition.y < 0)
			scrollPosition.y = 0;
	}

	// variables for keeping track of scrolling
	private Vector2 mouseLastPos;
	private bool mouseDragging = false;
	
	
	void Update()
	{
		if(Input.touches.Length > 0) 
		{
			Touch touch = Input.touches[0];
			if (IsInTournamentArea (touch.position) && touch.phase == TouchPhase.Moved)
			{
				// dragging
				ScrollTournament (touch.deltaPosition.y*3);
			}
		}
		
		if (Input.GetAxis("Mouse ScrollWheel") < 0)
		{
			ScrollTournament (MouseScrollStep);
		}
		else if (Input.GetAxis("Mouse ScrollWheel") > 0)
		{
			ScrollTournament (-MouseScrollStep);
		}
		
		if (Input.GetMouseButton(0) && IsInTournamentArea(Input.mousePosition))
		{
			if (mouseDragging)
			{
				ScrollTournament (Input.mousePosition.y - mouseLastPos.y);
			}
			mouseLastPos = Input.mousePosition;
			mouseDragging = true;
		}
		else
			mouseDragging = false;
	}
	private Vector2 buttonPos;  // Keeps track of where we've got to on the screen as we draw buttons

	void OnGUI()
	{        
		GUI.skin = MenuSkin;
		GUI.backgroundColor = new Color(0,0,0,0);
		if (!FB.IsLoggedIn)
		{  
		
			if (GUI.Button(new Rect((Screen.width/2)-120f , (Screen.height/2)-240f,200,150 ), ButtonTexture))
			{
				FB.Login("email,publish_actions", LoginCallback);
			}
			if (GUI.Button(new Rect((Screen.width/2)-150f , (Screen.height/2)-120,260,180 ), facebook))
			{
				FB.Login("email,publish_actions", LoginCallback);
			}
		}
		if (FB.IsLoggedIn)
		{
			string panelText = "Hi, ";
			panelText += (!string.IsNullOrEmpty(Manu_Username)) ? string.Format("{0}", Manu_Username) : "Dear";
			GUI.DrawTexture(new Rect((Screen.width/2)-250f , (Screen.height/2)-180f,300,200 ), ButtonTexture, ScaleMode.ScaleToFit, true, 1.0f);
			GUI.Label( (new Rect((Screen.width/2)+20f , (Screen.height/2)-180f, 260, 180)), panelText,f_style);
		}
		if (FB.IsLoggedIn) {  
						if (GUI.Button (new Rect ((Screen.width / 2) +10f, (Screen.height / 2) - 140, 260, 120), CHALLANGED)) {
								onChallengeClicked ();
						}
						if (GUI.Button (new Rect ((Screen.width / 2) +10f, (Screen.height / 2) - 40, 240,100), Brag)) {
								onBragClicked ();
						}	
		                    }
		#if UNITY_WEBPLAYER
		if (Screen.fullScreen)
		{
			if (DrawButton("Full Screen",FullScreenActiveTexture))
				SetFullscreenMode(false);
		}
		else 
		{
			if (DrawButton("Full Screen",FullScreenTexture))
				SetFullscreenMode(true);
		}
		#endif
	    DrawPopupMessage();
		}
	// Update is called once per frame
	public void AddPopupMessage(string message, float duration)
	{
		popupMessage = message;
		popupTime = Time.realtimeSinceStartup;
		popupDuration = duration;
	}
	public void DrawPopupMessage()
	{
		if (popupTime != 0 && popupTime + popupDuration > Time.realtimeSinceStartup)
		{
			// Show message that we sent a request
			Rect PopupRect = new Rect();
			PopupRect.width = 800;
			PopupRect.height = 100;
			PopupRect.x = Screen.width / 2 - PopupRect.width / 2;
			PopupRect.y = Screen.height / 2 - PopupRect.height / 2;
			GUI.Box(PopupRect,"",MenuSkin.GetStyle("box"));
			GUI.Label(PopupRect, popupMessage, MenuSkin.GetStyle("centred_text"));        
		}
		
	}

	void TournamentGui() 
	{
		GUILayout.BeginArea(new Rect((Screen.width - 450),0,450,Screen.height));
		
		// Title box
		GUI.Box   (new Rect(0,    - scrollPosition.y, 100,200), "",           MenuSkin.GetStyle("tournament_bar"));
		GUI.Label (new Rect(121 , - scrollPosition.y, 100,200), "Tournament", MenuSkin.GetStyle("heading"));
		
		Rect boxRect = new Rect();
		
		if(scores != null)
		{
			var x = 0;
			foreach(object scoreEntry in scores) 
			{
				Dictionary<string,object> entry = (Dictionary<string,object>) scoreEntry;
				Dictionary<string,object> user = (Dictionary<string,object>) entry["user"];
				
				string name     = ((string) user["name"]).Split(new char[]{' '})[0] + "\n";
				string score     = "Smashed: " + entry["score"];
				
				boxRect = new Rect(0, 121+(TournamentStep*x)-scrollPosition.y , 100,128);
				// Background box
				GUI.Box(boxRect,"",MenuSkin.GetStyle("tournament_entry"));
				
				// Text
				GUI.Label (new Rect(24, 136 + (TournamentStep * x) - scrollPosition.y, 100,128), (x+1)+".", MenuSkin.GetStyle("tournament_position"));      // Rank e.g. "1.""
				GUI.Label (new Rect(250,145 + (TournamentStep * x) - scrollPosition.y, 300,100), name, MenuSkin.GetStyle("tournament_name"));               // name   
				GUI.Label (new Rect(250,193 + (TournamentStep * x) - scrollPosition.y, 300,50), score, MenuSkin.GetStyle("tournament_score"));              // score
				Texture picture;
				if (friendImages.TryGetValue((string) user["id"], out picture)) 
				{
					GUI.DrawTexture(new Rect(118,128+(TournamentStep*x)-scrollPosition.y,115,115), picture);  // Profile picture
				}
				x++;
			}
			}
		else GUI.Label (new Rect(180,270,512,200), "Loading...", MenuSkin.GetStyle("text_only"));
		// Record length so we know how far we can scroll to
		tournamentLength = boxRect.y + boxRect.height + scrollPosition.y;
		
		GUILayout.EndArea();
	}
	private void onPlayClicked()
	{
		Util.Log("onPlayClicked");
		if (friends != null && friends.Count > 0)
		{
			// Select a random friend and get their picture
			Dictionary<string, string> friend = Util.RandomFriend(friends);
			GameStateManager.FriendName = friend["first_name"];
			GameStateManager.FriendID = friend["id"];
			LoadPicture(Util.GetPictureURL((string)friend["id"], 128, 128),FriendPictureCallback);
		}
		}
	private void onBragClicked()
	{
		Util.Log("onBragClicked");
		FB.Feed(
			linkCaption: "Hello in Xeemu world",
			picture: "https://lh6.googleusercontent.com/-q7hXjl9Gc-g/UklmeFeoeDI/AAAAAAAAAB8/kf17sHAfbYU/xeemu.jpg",
			linkName: "Checkout frends !",
			link: "http://apps.facebook.com/" + FB.AppId + "/?challenge_brag=" + (FB.IsLoggedIn ? FB.UserId : "guest")
			);
	}
	private void onChallengeClicked()                                                                                              
	{                                                                                                                              
		Util.Log("onChallengeClicked"); 
		string query = WWW.EscapeURL("SELECT uid, name, is_app_user, pic_square FROM user WHERE uid IN (SELECT uid2 FROM friend WHERE uid1 = me()) AND is_app_user = 1");
		string fql = "/fql?q="+query;
		if (GameStateManager.Score != 0 && GameStateManager.FriendID != null)                                                      
		{                                                                                                                          
			string[] recipient = { GameStateManager.FriendID };                                                                    
			FB.AppRequest(                                                                                                         
			              message: "Hello in Xeemu World" ,                  
			              to: recipient,                                                                                                     
			              filters : "",                                                                                                      
			              excludeIds : null,                                                                                                 
			              maxRecipients : null,                                                                                              
			              data: "{\"challenge_score\":" + "" + "}",                                           
			              title: "Send challenges to frends",                                                                                  
			              callback:appRequestCallback                                                                                        
			              );                                                                                                                 
		}                                                                                                                          
		else                                                                                                                       
		{                                                                                                                          
			FB.AppRequest(
				to: null,
				filters : "",
				excludeIds : null,
				message: "Hello in Xeemu World.",
				title: "Play with Xeemu",
				callback:appRequestCallback
				);                                                                                                                
		}                                                                                                                          
	} 
	private void appRequestCallback (FBResult result)
	{
		Util.Log("appRequestCallback");
		if (result != null)
		{
			var responseObject = Json.Deserialize(result.Text) as Dictionary<string, object>;
			object obj = 0;
			if (responseObject.TryGetValue ("cancelled", out obj))
			{
				Util.Log("Request cancelled");
			}
			else if (responseObject.TryGetValue ("request", out obj))
			{
				AddPopupMessage("Request Sent", ChallengeDisplayTime);
				
				Util.Log("Request sent");
			}
		}
	}
	public static void FriendPictureCallback(Texture texture)
	{
		GameStateManager.FriendTexture = texture;
	}
	delegate void LoadPictureCallback (Texture texture);
	IEnumerator LoadPictureEnumerator(string url, LoadPictureCallback callback)    
	{
		WWW www = new WWW(url);
		yield return www;
		callback(www.texture);
	}
	void LoadPicture (string url, LoadPictureCallback callback)
	{
		FB.API(url,Facebook.HttpMethod.GET,result =>
		       {
			if (result.Error != null)
			{
				Util.LogError(result.Error);
				return;
			}
			var imageUrl = Util.DeserializePictureURLString(result.Text);
			StartCoroutine(LoadPictureEnumerator(imageUrl,callback));
		});
	}
}
