using System.Collections.Generic;
using UnityEngine;

public class Rec {
    public int frameRate;
    public List<RecordingFrame> frames = new List<RecordingFrame>();

    public int totalFrames { get { return frames.Count; } }

    public float recordingLength { get { return totalFrames / frameRate; } }

    [System.Serializable]
    public class RecordingFrame {
        public List<AxisRec> inputs = new List<AxisRec>();
    }

    [System.Serializable]
    public class AxisRec  // represents state of certain input in one frame. Has to be class for inspector to serialize
    {
        public string axisName;    // from InputManager

        [HideInInspector]
        public float axisValue; // not raw value

        public AxisRec() {
            axisName = "";
            axisValue = 0f;
        }

        public AxisRec(AxisRec toCopy) {
            axisName = toCopy.axisName;
            axisValue = toCopy.axisValue;
        }

        public override bool Equals(object obj) {
            AxisRec other = obj as AxisRec;
            return Equals(other);
        }

        public bool Equals(AxisRec other) {
            if(other == null)
                return false;

            if(axisName != other.axisName)
                return false;

            return axisValue == other.axisValue;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }

    [System.Serializable]
    public class FrameProperty {
        public string name;
        public string property;

        public FrameProperty() {
            // for Json reader... 
        }

        public FrameProperty(string name, string property) {
            this.name = name;
            this.property = property;
        }
    }

    #region Constructors
    public Rec() {
        this.frameRate = 60;
        frames = new List<RecordingFrame>();
    }

    public Rec(int frameRate) {
        this.frameRate = Mathf.Max(1, frameRate);
        frames = new List<RecordingFrame>();
    }

    /// <summary>
    /// Copies the data in oldRecoding to a new instance of the <see cref="Recording"/> class.
    /// </summary>
    /// <param name='oldRecording'>
    /// Recording to be copied
    /// </param>
    public Rec(Rec oldRecording) {
        if(oldRecording != null) {
            frameRate = oldRecording.frameRate;
            frames = new List<RecordingFrame>(oldRecording.frames);
        } else {
            frameRate = 60;
            frames = new List<RecordingFrame>();
        }
    }
    #endregion

    /// <summary>
    /// Gets the closest frame index to a provided time
    /// </summary>
    /// <returns>
    /// The closest frame.
    /// </returns>
    /// <param name='toTime'>
    /// To time.
    /// </param>/
    public int GetClosestFrame(float toTime) {
        return (int)(toTime * frameRate);
    }

    /// <summary>
    /// Adds the supplied input info to given frame
    /// </summary>
    /// <param name='atFrame'>
    /// At frame.
    /// </param>
    /// <param name='inputInfo'>
    /// Input info.
    /// </param>
    public void AddInput(int atFrame, AxisRec inputInfo) {
        CheckFrame(atFrame);

        for(int i = 0; i < frames[atFrame].inputs.Count; i++) {
            // no duplicate properties
            if(frames[atFrame].inputs[i].axisName == inputInfo.axisName) {
                frames[atFrame].inputs[i] = new AxisRec(inputInfo);
                return;
            }
        }

        frames[atFrame].inputs.Add(new AxisRec(inputInfo));
    }

    /// <summary>
    /// Gets the given input at given frame.
    /// </summary>
    /// <returns>
    /// InputInfo
    /// </returns>
    /// <param name='atFrame'>
    /// At frame.
    /// </param>
    /// <param name='axisName'>
    /// Input name.
    /// </param>
    public AxisRec GetInput(int atFrame, string axisName) {
        if(atFrame < 0 || atFrame >= frames.Count) {
            Debug.LogWarning("Frame " + atFrame + " out of bounds");
            return null;
        } else {
            // iterating to find. Could avoid repeat access time with pre-processing, but would be a waste of memory/GC slowdown? & list is small anyway
            foreach(AxisRec input in frames[atFrame].inputs)
                if(input.axisName == axisName)
                    return input;
        }

        Debug.LogWarning("Input " + axisName + " not found in frame " + atFrame);
        return null;
    }

    /// <summary>
    /// Gets all inputs in a given frame.
    /// </summary>
    /// <returns>
    /// The inputs.
    /// </returns>
    /// <param name='atFrame'>
    /// At frame.
    /// </param>
    public AxisRec[] GetInputs(int atFrame) {
        if(atFrame >= frames.Count) { return new AxisRec[0]; }

        if(atFrame < 0) {
            Debug.LogWarning("Frame " + atFrame + " out of bounds");
            return new AxisRec[0];
        } else
            return frames[atFrame].inputs.ToArray();
    }

    // Make sure this frame has an entry
    void CheckFrame(int frame) {
        while(frame >= frames.Count)
            frames.Add(new RecordingFrame());
    }
}