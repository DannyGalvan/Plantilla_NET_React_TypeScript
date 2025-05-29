import { Component } from "react";

interface ErrorBoundaryProps {
  readonly children: React.ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  static getDerivedStateFromError(error: Error) {
    // Actualiza el estado para que el próximo renderizado muestre el fallback UI.
    return { hasError: true, error };
  }

  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  shouldComponentUpdate(
    nextProps: Readonly<ErrorBoundaryProps>,
    nextState: Readonly<ErrorBoundaryState>,
  ): boolean {
    const { hasError, error } = this.state;
    const { children } = this.props;
    return (
      nextState.hasError !== hasError ||
      nextState.error !== error ||
      nextProps.children !== children
    );
  }

  render() {
    const { hasError, error } = this.state;
    const { children } = this.props;

    if (hasError) {
      // Puedes personalizar la UI de fallback que se muestra cuando ocurre un error.
      return (
        <section className="flex overflow-y-auto flex-col flex-wrap justify-around p-8 h-screen text-red-600">
          <header className="text-3xl text-center">
            <p>Hubo un error en la aplicación. {error?.message}</p>
          </header>
          <main className="text-center">
            <article>
              <p className="text-2xl">
                Contacte con el administrador del sistema
              </p>
              <p>
                De ser posible envie una imagen con este error, cierre sesion y
                vuelva a iniciar la pagina
              </p>
            </article>
            <article className="mt-8">
              <p className="text-xl">Lamentamos los inconvenientes</p>
              <p className="font-bold"> </p>
            </article>
            <a
              className="cursor-pointer block mt-4"
              href="mailto:Cgalvan@itglobal.com.gt"
            >
              contactarse a este correo: Cgalvan@itglobal.com.gt
            </a>
            <a className="block mt-2" href="/login">
              Regresar a inicio
            </a>
          </main>
          <footer className="text-center">
            <span>Name. {error?.name}</span>
            <span>Error: {error?.message}</span>
            <span>Stack. {error?.stack}</span>
          </footer>
        </section>
      );
    }

    return children;
  }
}

export default ErrorBoundary;
