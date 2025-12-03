unit Emetra.VclUtil.Settings.Interfaces;

interface

uses
  Vcl.ExtCtrls, Vcl.Graphics, System.Classes;

type
  /// <summary>
  ///   Interface that GUI element (typically a form or a frame) can call when
  ///   form elements needs to be saved or restored.
  /// </summary>
  IGuiSettings = interface
    ['{D6043F62-4A7A-4A58-8187-A34602636CEC}']
    function TryGetFont( out AFontName: string; out AFontSize: integer ): boolean;
    function TryGetColor( out AColor: TColor ): boolean;
    procedure SaveFont( const AFontName: string; const AFontSize: integer );
    procedure SaveColor( const AColor: TColor );
    procedure RestoreFormState;
    procedure SaveFormState;
    procedure RestorePanelHeight( APanel: TCustomPanel; AIniKey: string = '' );
    procedure RestorePanelWidth( APanel: TCustomPanel; AIniKey: string = '' );
    procedure RestoreSplitter( ASplitter: TComponent; AIniKey: string = '' );
    procedure SavePanelHeight( APanel: TCustomPanel; AIniKey: string = '' );
    procedure SavePanelWidth( APanel: TCustomPanel; AIniKey: string = '' );
    procedure SaveSplitter( ASplitter: TComponent; AIniKey: string = '' );
  end;

  /// <summary>
  ///   A GUI element can implement this interface to allow automatic saving of
  ///   its properties.
  /// </summary>
  IGuiSaveRestoreSettings = interface
    ['{979ED957-26DA-40C7-A667-EB1C2E695E9C}']
    procedure RestoreState( ASettings: IGuiSettings );
    procedure SaveState( ASettings: IGuiSettings );
  end;

implementation

end.
