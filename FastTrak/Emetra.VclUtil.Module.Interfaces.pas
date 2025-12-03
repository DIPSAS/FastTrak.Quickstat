unit Emetra.VclUtil.Module.Interfaces;

interface

uses
  { Emetra.VclUtil }
  Emetra.VclUtil.Style.Interfaces,
  { Standard  }
  System.Classes, Vcl.Controls;

type
  IGuiItemSelector = interface
    ['{205522FB-DA12-47E9-B064-ED171D3931D9}']
    function SelectInteger( const AHeading, ADetail: string; AItems: TStrings ): integer;
    function SelectString( const AHeading, ADetail: string; AItems: TStrings ): string;
  end;

  IGuiModule = interface( IGuiStyleObserver )
    ['{F912C350-E193-47DA-8D72-F93C244E529B}']
    { Usable allows the controller to hide unusable components, eg. not supported by database etc }
    function Usable: Boolean;
    { Control must return the outermost container element of the module }
    function Control: TWinControl;
    { An end-user-friendly name, suitable for showing in a progress indicator etc. }
    function FriendlyName: string;
    { Prepare puts a module on a parent control }
    procedure Prepare( AParent: TWinControl; const ALayout: TAlign );
  end;

  IGuiInputFrame = interface
    ['{9B0AF1D2-A3BD-40A8-A441-BF471CF3CC05}']
    { Brings input focus to the frame }
    procedure FocusHere;
    { Prepare puts a module on a parent control }
    procedure Prepare( AParent: TWinControl; const ALayout: TAlign );
  end;

implementation

end.
