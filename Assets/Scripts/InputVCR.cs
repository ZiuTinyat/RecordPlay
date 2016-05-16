/* InputVCR.cs
 * Copyright Eddie Cameron 2012 (See readme for licence)
 * ----------
 * Place on any object you wish to use to record or playback any inputs for
 * Switch modes to change current behaviour
 *   - Passthru : object will use live input commands from player
 *   - Record : object will use, as well as record, live input commands from player
 *   - Playback : object will use either provided input string or last recorded string rather than live input
 *   - Pause : object will take no input (buttons/axis will be frozen in last positions)
 * 
 * -----------
 * Recs are all saved to the 'currentRec' member, which you can get with GetRec(). This can then be copied 
 * to a new Rec object to be saved and played back later.
 * Call ToString() on these Recs to get a text version of this if you want to save a Rec after the program exits.
 * -----------
 * To use, place in a gameobject, and have all scripts in the object refer to it instead of Input.
 * 
 * eg: instead of Input.GetButton( "Jump" ), you would use vcr.GetButton( "Jump" ), where vcr is a 
 * reference to the component in that object
 * If VCR is in playback mode, and the "Jump" input was recorded, it will give the recorded input state, 
 * otherwise it will just pass through the live input state
 * 
 * Note, InputVCR can't be statically referenced like Input, since you may have multiple objects playing
 * different Recs, or an object playing back while another is taking live input...
 * ----------
 * Use this snippet in scripts you wish to replace Input with InputVCR, so they can be used in objects without a VCR as well:
 
  private bool useVCR;
  private InputVCR vcr;
  
  void Awake()
  {
    Transform root = transform;
	while ( root.parent != null )
		root = root.parent;
	vcr = root.GetComponent<InputVCR>();
	useVCR = vcr != null;
  }
  
 * Then Replace any input lines with:
  
  if ( useVCR )
  	<some input value> = vcr.GetSomeInput( "someaxisName" );
  else
  	<some input value> = Input.GetSomeInput( "someaxisName" );
  
 * Easy! 
 * -------------
 * More information and tools at grapefruitgames.com, @eddiecameron, or support@grapefruitgames.com
 * 
 * This script is open source under the GNU LGPL licence. Do what you will with it! 
 * http://www.gnu.org/licenses/lgpl.txt
 * 
 */
using UnityEngine;
using System.Collections.Generic;
using CnControls;

public class InputVCR : MonoBehaviour
{
	#region Inspector properties
    public Rec.AxisRec[] inputsToRecord;
// list of axis and button names ( from Input manager) that should be recorded
	
    public int RecFrameRate = 60;

    public PlayerMoveController pmc;

    [SerializeField]
	private InputVCRMode _mode = InputVCRMode.Passthru; // initial mode that vcr is operating in
	public InputVCRMode mode
	{
		get { return _mode; }
	}
	#endregion

	float realRecTime;
	
	Rec currentRec;		// the Rec currently in the VCR. Copy or ToString() this to save.
	public float currentTime{
		get {
			return currentFrame / (float)currentFrameRate; }
	}
	public int currentFrameRate{
		get {
			if ( currentRec == null )
				return RecFrameRate;
			else
				return currentRec.frameRate;
		}
	}
	public int currentFrame{ get; private set; }	// current frame of Rec/playback
	
	Queue<Rec.FrameProperty> nextPropertiesToRecord = new Queue<Rec.FrameProperty>();	// if SyncLocation or SyncProperty are called, this will hold their results until the recordstring is next written to
		
	Dictionary<string, Rec.AxisRec> lastFrameInputs = new Dictionary<string, Rec.AxisRec>();	// list of inputs from last frame (for seeing what buttons have changed state)
	Dictionary<string, Rec.AxisRec> thisFrameInputs = new Dictionary<string, Rec.AxisRec>();
		
	float playbackTime;
	
	public event System.Action finishedPlayback;	// sent when playback finishes
	

    public void Awake() {
        pmc = GameObject.Find("Sphere").GetComponent<PlayerMoveController>();
    }

	/// <summary>
	/// Start Rec. Will append to already started Rec
	/// </summary>
	public void Record()
	{
		if ( currentRec == null || currentRec.recordingLength == 0 )
			NewRecording();
		else
			_mode = InputVCRMode.Record;
	}
	
	/// <summary>
	/// Starts a new Rec. If old Rec wasn't saved it will be lost forever!
	/// </summary>
	public void NewRecording()
	{
		// start Rec live input
		currentRec = new Rec( RecFrameRate );
		currentFrame = 0;
		realRecTime = 0;
		
		nextPropertiesToRecord.Clear ();
		
		_mode = InputVCRMode.Record;
	}
	
	/// <summary>
	/// Start playing back the current Rec, if present.
	/// If currently paused, will just resume
	/// </summary>
	public void Play()
	{
		// if currently paused during playback, will continue
		if ( mode == InputVCRMode.Pause )
			_mode = InputVCRMode.Playback;
		else
		{
			// if not given any input string, will use last Rec
			Play ( currentRec );
		}
	}
	
	/// <summary>
	/// Play the specified Rec, from optional specified time
	/// </summary>
	/// <param name='Rec'>
	/// Rec.
	/// </param>
	/// <param name='startRecFromTime'>
	/// OPTIONAL: Time to start Rec at
	/// </param>
	public void Play( Rec Rec, float startRecFromTime = 0 )
	{	
		currentRec = Rec;
		currentFrame = Rec.GetClosestFrame ( startRecFromTime );
		
		thisFrameInputs.Clear ();
		lastFrameInputs.Clear ();
		
		_mode = InputVCRMode.Playback;
		playbackTime = startRecFromTime;
	}
	
	/// <summary>
	/// Pause Rec or playback. All input will be blocked while paused
	/// </summary>
	public void Pause()
	{
		_mode = InputVCRMode.Pause;
	}
	
	/// <summary>
	/// Stop Rec or playback and rewind Live input will be passed through
	/// </summary>
	public void Stop()
	{			
		_mode = InputVCRMode.Passthru;
		currentFrame = 0;
		playbackTime = 0;
        pmc.PlayBackEnd();
	}
	
	/// <summary>
	/// Gets a copy of the current Rec
	/// </summary>
	/// <returns>
	/// The Rec.
	/// </returns>
	public Rec GetRec()
	{
        return currentRec;
	}
	
	void FixedUpdate()
	{	
		if ( _mode == InputVCRMode.Playback )
		{
			// update last frame and this frame
			// this way, all changes are transmitted, even if a button press lasts less than a frame (like in Input)
			lastFrameInputs = thisFrameInputs;
			
			int lastFrame = currentFrame;
			currentFrame = currentRec.GetClosestFrame ( playbackTime );
			
			if ( currentFrame > currentRec.totalFrames )
			{
				// end of Recording
				if ( finishedPlayback != null )
					finishedPlayback( );
				Stop ();
			}
			else
			{
				// go through all changes in recorded input since last frame
				var changedInputs = new Dictionary<string, Rec.AxisRec>();
				for( int frame = lastFrame + 1; frame <= currentFrame; frame++ )
                {
					foreach( var input in currentRec.GetInputs ( frame ) )
					{
						// thisFrameInputs only updated once per game frame, so all changes, no matter how brief, will be marked
						if ( !thisFrameInputs.ContainsKey ( input.axisName ) || !thisFrameInputs[input.axisName].Equals( input ) )
						{
							if ( changedInputs.ContainsKey ( input.axisName ) )
								changedInputs[input.axisName] = input;
							else
								changedInputs.Add( input.axisName, input );
						}
					}
				}
				
				// update input to be used tihs frame
				foreach( var changedInput in changedInputs )
				{
					if ( thisFrameInputs.ContainsKey ( changedInput.Key ) )
						thisFrameInputs[changedInput.Key] = changedInput.Value;
					else
						thisFrameInputs.Add ( changedInput.Key, changedInput.Value );
				}
				
				playbackTime += Time.deltaTime;
			}
		}
		else if ( _mode == InputVCRMode.Record )
		{	
			realRecTime += Time.deltaTime;
			// record current input to frames, until Rec catches up with realtime
			while ( currentTime < realRecTime )
			{
				// and keycodes & buttons defined in inputsToRecord
				foreach( var input in inputsToRecord )
				{
                    input.axisValue = CnInputManager.GetAxis( input.axisName );

                    currentRec.AddInput ( currentFrame, input );
				}
				
				currentFrame++;
			}
		}
	}
	
	// These methods replace those in Input, so that this object can ignore whether it is record
	#region Input replacements
	public float GetAxis( string axisName )
	{
		if ( _mode == InputVCRMode.Pause )
			return 0;
		
		if ( _mode == InputVCRMode.Playback && thisFrameInputs.ContainsKey( axisName ) )
			return thisFrameInputs[axisName].axisValue;
		else
			return Input.GetAxis ( axisName );
	}
	#endregion
}

public enum InputVCRMode
{
	Passthru,	// normal input
	Record,
	Playback,
	Pause
}
