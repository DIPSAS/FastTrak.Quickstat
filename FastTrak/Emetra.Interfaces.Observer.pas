unit Emetra.Interfaces.Observer;

interface

type
  IListener = interface
    ['{1E0CFE05-0FC6-4145-9FB9-4F92433BA1FB}']
    procedure AfterUpdate( Sender: TObject );
  end;

  IObservable = interface
    ['{B8138904-D1A3-4B65-A40E-09FC37BF4BDC}']
    procedure Attach( AObserver: IListener );
    procedure Detach( AObserver: IListener );
    procedure DetachAll;
    procedure BeginUpdate;
    procedure EndUpdate;
  end;

implementation

end.
