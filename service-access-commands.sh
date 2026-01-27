# Quick service type change commands

# Option A: Patch existing service to LoadBalancer
kubectl patch svc fplbot -n fplbot -p '{"spec":{"type":"LoadBalancer"}}'

# Option B: Change to NodePort with specific port
kubectl patch svc fplbot -n fplbot -p '{"spec":{"type":"NodePort","ports":[{"port":80,"targetPort":80,"nodePort":30080}]}}'

# Option C: Helm upgrade to change service type
helm upgrade fplbot ./helm/fplbot \
  --namespace fplbot \
  --reuse-values \
  --set service.type=LoadBalancer

# Check the result
kubectl get svc fplbot -n fplbot