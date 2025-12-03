unit Emetra.Interfaces.ListBox;

interface

uses
  Classes, Vcl.Graphics;

type
  IListBoxBase = interface ['{A83E9E86-A608-4A19-9BB2-B353E34B71E7}']
    function V: string;  { Code value, painted in blue usually }
    function DN: string; { Code name, painted in black }
    function OT: string; { Other text, painted in red }
    function IsCurrent: boolean;
    function AsListBox(const ASimple: boolean = true): string;
  end;

  IListBoxItem = interface(IListBoxBase)
    ['{BFBAEE2B-3BEF-427C-8DD4-E56330192C3C}']
    function Description: string;
    function Match(const AFilterText: string): boolean; { Compatible with signature of IMatch in Lookup interfaces }
  end;

  IListBoxStatusColor = interface
    ['{6DF0FF17-2F18-4036-A37D-7D21155E9DA8}']
    function StatusColor: TColor;
  end;

  IListBoxBackgroundColor = interface
    ['{A02230B7-C991-43B5-9DF4-0FFD7FAF7771}']
    function Color: TColor;
  end;

  IListBoxDetails = interface['{E0F2F33E-82EA-42FB-8815-BD88A0822B52}']
    function GreenText: string;
    function BlueText: string;
  end;

  IListBoxStrikeout = interface['{51F92632-AD98-4643-B679-08672406AFCB}']
    function Strikeout: boolean;
  end;

implementation

end.
