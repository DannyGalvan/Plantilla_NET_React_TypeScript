export function LoadingComponent() {
  return (
    <div className="container">
      <div className="flex flex-col items-center">
        <div className="lds-roller">
          <div />
          <div />
          <div />
          <div />
          <div />
          <div />
          <div />
          <div />
        </div>
        <h3 className="font-bold text-center">Cargando...</h3>
      </div>
    </div>
  );
}
