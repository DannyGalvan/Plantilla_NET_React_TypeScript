import { Outlet, useNavigation } from "react-router";

import { useAuth } from "../hooks/useAuth";
import LoadingPage from "../pages/public/LoadingPage";

import { Layout } from "./Layout";
import { LayoutLogin } from "./LayoutLogin";

export function Root() {
  const navigation = useNavigation();
  const { isLoggedIn } = useAuth();

  return isLoggedIn ? (
    <Layout>
      {navigation.state === "loading" ? <LoadingPage /> : <Outlet />}
    </Layout>
  ) : (
    <LayoutLogin>
      {navigation.state === "loading" ? <LoadingPage /> : <Outlet />}
    </LayoutLogin>
  );
}
