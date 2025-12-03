unit Emetra.Interfaces.Lookup;

interface

type
  { Names based on KITH standard }
  ICodedString = interface
    ['{B8006622-230D-4C78-A85B-E81D5881BC46}']
    function V: string;
    function DN: string;
  end;

  ICodedValue = interface( ICodedString )
    ['{6082BB79-1937-4986-98CA-5F06F80A965C}']
    function OT: string;
  end;

  IMatchable = interface
    ['{BAAAC2AB-4F14-4142-8588-08A0F6C7DCA2}']
    function Match( const AFilterText: string ): boolean;
  end;

implementation

end.
