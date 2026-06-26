import Swal, {
  type SweetAlertIcon,
  type SweetAlertOptions,
  type SweetAlertResult,
} from "sweetalert2";

const pixelAlertClasses = {
  popup: "chronos-alert",
  title: "chronos-alert-title",
  htmlContainer: "chronos-alert-body",
  timerProgressBar: "chronos-alert-progress",
  confirmButton: "chronos-alert-button chronos-alert-confirm",
  cancelButton: "chronos-alert-button chronos-alert-cancel",
  icon: "chronos-alert-icon",
};

const baseAlertOptions: SweetAlertOptions = {
  background: "#0b1523",
  color: "#f6f1dc",
  buttonsStyling: false,
  customClass: pixelAlertClasses,
};

export function showPixelToast(options: {
  icon: SweetAlertIcon;
  title: string;
  text: string;
}): Promise<SweetAlertResult> {
  return Swal.fire({
    ...baseAlertOptions,
    toast: true,
    position: "bottom-end",
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
    ...options,
  });
}

export function showPixelConfirm(options: {
  title: string;
  text: string;
  confirmButtonText: string;
}): Promise<SweetAlertResult> {
  return Swal.fire({
    ...baseAlertOptions,
    icon: "warning",
    showCancelButton: true,
    cancelButtonText: "Cancel",
    ...options,
  });
}

export function showPixelAlert(options: {
  icon: SweetAlertIcon;
  title: string;
  text: string;
  timer?: number;
}): Promise<SweetAlertResult> {
  return Swal.fire({
    ...baseAlertOptions,
    showConfirmButton: options.timer == null,
    timer: options.timer,
    ...options,
  });
}
