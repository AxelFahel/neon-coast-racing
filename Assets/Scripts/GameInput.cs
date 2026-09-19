using UnityEngine;
using UnityEngine.InputSystem;
namespace NeonCoast {
/// <summary>Unified input layer — reads from keyboard and gamepad simultaneously.
/// Gamepad analog values take priority when the stick/trigger is moved beyond deadzone.</summary>
public static class GameInput {

 // --- Driving (continuous axes) ---

 /// <summary>-1 (brake/reverse) to +1 (accelerate). Gamepad: RT/LT analog triggers.</summary>
 public static float Throttle { get {
  var k=Keyboard.current; var g=Gamepad.current;
  float kb=0; if(k!=null) kb=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0);
  float gp=0; if(g!=null) gp=g.rightTrigger.ReadValue()-g.leftTrigger.ReadValue();
  return Mathf.Abs(gp)>.02f?gp:kb;
 }}

 /// <summary>-1 (left) to +1 (right). Gamepad: left stick X with deadzone.</summary>
 public static float Steer { get {
  var k=Keyboard.current; var g=Gamepad.current;
  float kb=0; if(k!=null) kb=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
  float gp=0; if(g!=null) gp=g.leftStick.x.ReadValue();
  return Mathf.Abs(gp)>.1f?gp:kb;
 }}

 // --- Driving (held buttons) ---

 /// <summary>Handbrake / drift. Keyboard: Space. Gamepad: X / Square.</summary>
 public static bool Handbrake { get {
  var k=Keyboard.current; var g=Gamepad.current;
  return (k!=null&&k.spaceKey.isPressed)||(g!=null&&g.buttonWest.isPressed);
 }}

 /// <summary>Nitro boost. Keyboard: Shift. Gamepad: A / Cross or RB.</summary>
 public static bool Boost { get {
  var k=Keyboard.current; var g=Gamepad.current;
  return (k!=null&&(k.leftShiftKey.isPressed||k.rightShiftKey.isPressed))||(g!=null&&(g.buttonSouth.isPressed||g.rightShoulder.isPressed));
 }}

 // --- Driving (press-once buttons) ---

 /// <summary>Camera toggle. Keyboard: C. Gamepad: Y / Triangle.</summary>
 public static bool CameraToggle { get {
  var k=Keyboard.current; var g=Gamepad.current;
  return (k!=null&&k.cKey.wasPressedThisFrame)||(g!=null&&g.buttonNorth.wasPressedThisFrame);
 }}

 /// <summary>Recover at checkpoint. Keyboard: R. Gamepad: B / Circle.</summary>
 public static bool Reset { get {
  var k=Keyboard.current; var g=Gamepad.current;
  return (k!=null&&k.rKey.wasPressedThisFrame)||(g!=null&&g.buttonEast.wasPressedThisFrame);
 }}

 // --- UI / Menu ---

 /// <summary>Pause toggle. Keyboard: Escape. Gamepad: Start / Menu.</summary>
 public static bool Pause { get {
  var k=Keyboard.current; var g=Gamepad.current;
  return (k!=null&&k.escapeKey.wasPressedThisFrame)||(g!=null&&g.startButton.wasPressedThisFrame);
 }}

 /// <summary>Confirm / proceed. Keyboard: Enter. Gamepad: A / Cross or Start.</summary>
 public static bool Confirm { get {
  var k=Keyboard.current; var g=Gamepad.current;
  return (k!=null&&k.enterKey.wasPressedThisFrame)||(g!=null&&(g.buttonSouth.wasPressedThisFrame||g.startButton.wasPressedThisFrame));
 }}

 /// <summary>Back / cancel. Keyboard: Escape. Gamepad: B / Circle or Select.</summary>
 public static bool Back { get {
  var k=Keyboard.current; var g=Gamepad.current;
  return (k!=null&&k.escapeKey.wasPressedThisFrame)||(g!=null&&(g.buttonEast.wasPressedThisFrame||g.selectButton.wasPressedThisFrame));
 }}

 /// <summary>True when any gamepad is currently connected.</summary>
 public static bool GamepadConnected => Gamepad.current!=null;
}
}
