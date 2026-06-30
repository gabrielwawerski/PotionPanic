function isExternalUrl(value) {
  return /^(?:[a-z]+:)?\/\//i.test(value)
    || value.startsWith("mailto:")
    || value.startsWith("tel:")
    || value.startsWith("#");
}

export function normalizeSiteBase(base = "/") {
  const normalized = `${base ?? "/"}`.trim();
  if (!normalized || normalized === "/") {
    return "/";
  }

  return `/${normalized.replace(/^\/+|\/+$/g, "")}/`;
}

export function stripSiteBase(value, base = "/") {
  const normalizedValue = `${value ?? ""}`.trim();
  if (!normalizedValue || isExternalUrl(normalizedValue)) {
    return normalizedValue;
  }

  const rootPath = normalizedValue.startsWith("/")
    ? normalizedValue
    : `/${normalizedValue.replace(/^\/+/, "")}`;
  const normalizedBase = normalizeSiteBase(base);
  if (normalizedBase === "/") {
    return rootPath;
  }

  const basePath = normalizedBase.slice(0, -1);
  if (rootPath === basePath) {
    return "/";
  }

  if (rootPath.startsWith(`${basePath}/`)) {
    return rootPath.slice(basePath.length) || "/";
  }

  return rootPath;
}

export function withSiteBase(value, base = "/") {
  const normalizedValue = `${value ?? ""}`.trim();
  if (!normalizedValue || isExternalUrl(normalizedValue)) {
    return normalizedValue;
  }

  const normalizedBase = normalizeSiteBase(base);
  const rootPath = stripSiteBase(normalizedValue, normalizedBase);
  if (normalizedBase === "/") {
    return rootPath.startsWith("/")
      ? rootPath
      : `/${rootPath.replace(/^\/+/, "")}`;
  }

  const basePath = normalizedBase.slice(0, -1);
  if (rootPath === "/") {
    return normalizedBase;
  }

  return `${basePath}/${rootPath.replace(/^\/+/, "")}`;
}
