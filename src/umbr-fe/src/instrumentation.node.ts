import { OTLPMetricExporter } from "@opentelemetry/exporter-metrics-otlp-grpc";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-grpc";
import { resourceFromAttributes } from "@opentelemetry/resources";
import { PeriodicExportingMetricReader } from "@opentelemetry/sdk-metrics";
import { NodeSDK } from "@opentelemetry/sdk-node";
import { ATTR_SERVICE_NAME } from "@opentelemetry/semantic-conventions";
import { getNodeAutoInstrumentations } from "@opentelemetry/auto-instrumentations-node";
import Pyroscope from "@pyroscope/nodejs";

const serviceName = process.env.OTEL_SERVICE_NAME ?? "umbraco-web";
const deploymentEnvironment = process.env.OTEL_DEPLOYMENT_ENVIRONMENT ?? "demo";

const pyroscopeServer =
  process.env.PYROSCOPE_SERVER_ADDRESS ?? "http://pyroscope.observability.svc.cluster.local:4040";

Pyroscope.init({
  serverAddress: pyroscopeServer,
  appName: serviceName,
  tags: {
    "deployment.environment": deploymentEnvironment,
  },
});

Pyroscope.start();

const sdk = new NodeSDK({
  resource: resourceFromAttributes({
    [ATTR_SERVICE_NAME]: serviceName,
    "deployment.environment": deploymentEnvironment,
    "service.namespace": "demo-services",
  }),
  traceExporter: new OTLPTraceExporter(),
  metricReader: new PeriodicExportingMetricReader({
    exporter: new OTLPMetricExporter(),
  }),
  instrumentations: [
    getNodeAutoInstrumentations({
      "@opentelemetry/instrumentation-fs": { enabled: false },
    }),
  ],
});

sdk.start();
