import { Images } from "../../assets/images/images";
import { Col } from "../../components/grid/Col";

function LoadingPage() {
  return (
    <div className="flex h-[100vh] flex-row items-center">
      <div className="container mx-auto">
        <div className="flex flex-row justify-center">
          <Col md={4} sm={4} xs={8}>
            <img
              alt="logo"
              className="loading w-100 rounded-xl object-cover"
              src={Images.logo}
            />
            <h3 className="text-center text-3xl text-gray-500 loading">
              Cargando... Espere
            </h3>
          </Col>
        </div>
      </div>
    </div>
  );
}

export default LoadingPage;
